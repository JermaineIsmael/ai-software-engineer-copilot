using System.Security.Cryptography;
using System.Text;
using Copilot.Api.Configuration;
using Copilot.Api.Models.Workspace;
using Microsoft.Extensions.Options;
using WorkspaceModel = Copilot.Api.Models.Workspace.Workspace;

namespace Copilot.Api.Services.Workspace;

public sealed class PatchApplicationService : IPatchApplicationService
{
    private readonly IWorkspaceManager _workspaceManager;
    private readonly WorkspaceOptions _options;
    private readonly ILogger<PatchApplicationService> _logger;

    public PatchApplicationService(
        IWorkspaceManager workspaceManager,
        IOptions<WorkspaceOptions> options,
        ILogger<PatchApplicationService> logger)
    {
        ArgumentNullException.ThrowIfNull(workspaceManager);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);

        _workspaceManager = workspaceManager;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<PatchResult> ApplyAsync(
        Guid workspaceId,
        Patch patch,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(patch);
        ArgumentNullException.ThrowIfNull(patch.Files);

        if (patch.Files.Count == 0)
        {
            throw new ArgumentException(
                "A patch must contain at least one file operation.",
                nameof(patch));
        }

        if (_options.MaximumPatchFiles <= 0 ||
            _options.MaximumPatchBytes <= 0)
        {
            throw new InvalidOperationException(
                "Workspace patch limits must be greater than zero.");
        }

        if (patch.Files.Count > _options.MaximumPatchFiles)
        {
            throw new ArgumentException(
                $"A patch cannot contain more than {_options.MaximumPatchFiles} file operations.",
                nameof(patch));
        }

        await using var lease = await _workspaceManager.AcquireAsync(
            workspaceId,
            cancellationToken);

        var workspace = lease.Workspace;
        var comparer = OperatingSystem.IsWindows()
            ? StringComparer.OrdinalIgnoreCase
            : StringComparer.Ordinal;
        var paths = new HashSet<string>(comparer);
        var validated = new List<ValidatedModification>(patch.Files.Count);
        long patchBytes = 0;

        foreach (var modification in patch.Files)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (modification is null)
            {
                throw new ArgumentException(
                    "Patch file operations cannot be null.",
                    nameof(patch));
            }

            if (!Enum.IsDefined(modification.Operation))
            {
                throw new ArgumentException(
                    $"Unsupported file operation '{modification.Operation}'.",
                    nameof(patch));
            }

            var targetPath = WorkspacePathResolver.Resolve(
                workspace.WorkingDirectory,
                modification.Path);

            if (!paths.Add(targetPath))
            {
                throw new ArgumentException(
                    $"A patch cannot contain multiple operations for '{modification.Path}'.",
                    nameof(patch));
            }

            var exists = File.Exists(targetPath);

            if (Directory.Exists(targetPath))
            {
                throw new ArgumentException(
                    $"The path '{modification.Path}' refers to a directory.",
                    nameof(patch));
            }

            if (modification.Operation == FileOperation.Create && exists)
            {
                throw new InvalidOperationException(
                    $"Cannot create '{modification.Path}' because it already exists.");
            }

            if (modification.Operation is FileOperation.Update or FileOperation.Delete &&
                !exists)
            {
                throw new InvalidOperationException(
                    $"Cannot {modification.Operation.ToString().ToLowerInvariant()} '{modification.Path}' because it does not exist.");
            }

            if (modification.Operation is FileOperation.Create or FileOperation.Update)
            {
                if (modification.Content is null)
                {
                    throw new ArgumentException(
                        $"The '{modification.Operation}' operation requires file content.",
                        nameof(patch));
                }
            }
            else if (modification.Content is not null)
            {
                throw new ArgumentException(
                    "The delete operation cannot include file content.",
                    nameof(patch));
            }

            var contentBytes = modification.Content is null
                ? null
                : Encoding.UTF8.GetBytes(modification.Content);
            var contentLength = contentBytes?.LongLength ?? 0;
            var remainingBytes = _options.MaximumPatchBytes -
                patchBytes -
                contentLength;

            if (remainingBytes < 0)
            {
                throw new ArgumentException(
                    $"Patch content and rollback data exceed the configured limit of {_options.MaximumPatchBytes} bytes.",
                    nameof(patch));
            }

            var originalBytes = exists
                ? await ReadBoundedFileAsync(
                    targetPath,
                    remainingBytes,
                    cancellationToken)
                : null;
            patchBytes = checked(
                patchBytes +
                (originalBytes?.LongLength ?? 0) +
                contentLength);

            if (!string.IsNullOrWhiteSpace(modification.ExpectedContentHash))
            {
                if (!IsSha256Hash(modification.ExpectedContentHash))
                {
                    throw new ArgumentException(
                        $"The expected content hash for '{modification.Path}' must be a SHA-256 hex value.",
                        nameof(patch));
                }

                if (originalBytes is null ||
                    !Hash(originalBytes).Equals(
                        modification.ExpectedContentHash,
                        StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException(
                        $"The file '{modification.Path}' changed since the patch was generated.");
                }
            }
            else if ((modification.Operation is
                          FileOperation.Update or FileOperation.Delete) &&
                     !await MatchesBaselineAsync(
                         workspace,
                         modification.Path,
                         originalBytes,
                         cancellationToken))
            {
                throw new InvalidOperationException(
                    $"The file '{modification.Path}' has changed from its workspace baseline; supply its current SHA-256 hash to confirm the patch.");
            }

            validated.Add(
                new ValidatedModification(
                    modification,
                    targetPath,
                    originalBytes,
                    contentBytes));
        }

        var applied = new List<ValidatedModification>();
        var temporaryFiles = new List<string>();

        try
        {
            foreach (var modification in validated)
            {
                cancellationToken.ThrowIfCancellationRequested();
                applied.Add(modification);

                if (modification.ContentBytes is not null)
                {
                    var parentDirectory =
                        Path.GetDirectoryName(modification.TargetPath)
                        ?? throw new InvalidOperationException(
                            "The target file has no parent directory.");
                    Directory.CreateDirectory(parentDirectory);
                    _ = WorkspacePathResolver.Resolve(
                        workspace.WorkingDirectory,
                        modification.Request.Path);

                    var temporaryPath = Path.Combine(
                        parentDirectory,
                        $".copilot-{Guid.NewGuid():N}.tmp");

                    await using (var stream = new FileStream(
                                     temporaryPath,
                                     FileMode.CreateNew,
                                     FileAccess.Write,
                                     FileShare.None,
                                     bufferSize: 81920,
                                     FileOptions.Asynchronous))
                    {
                        temporaryFiles.Add(temporaryPath);
                        await stream.WriteAsync(
                            modification.ContentBytes,
                            cancellationToken);
                        await stream.FlushAsync(cancellationToken);
                    }

                    File.Move(
                        temporaryPath,
                        modification.TargetPath,
                        overwrite: modification.Request.Operation ==
                            FileOperation.Update);
                    temporaryFiles.Remove(temporaryPath);
                    modification.Applied = true;
                }
                else
                {
                    if (!File.Exists(modification.TargetPath))
                    {
                        throw new InvalidOperationException(
                            $"The file '{modification.Request.Path}' changed before it could be deleted.");
                    }

                    File.Delete(modification.TargetPath);
                    modification.Applied = true;
                }
            }
        }
        catch (Exception applyException)
        {
            var rollbackExceptions =
                await RollbackAsync(
                    applied,
                    workspace.WorkingDirectory);

            if (rollbackExceptions.Count > 0)
            {
                _logger.LogError(
                    applyException,
                    "Patch application failed and rollback was incomplete for workspace {WorkspaceId}.",
                    workspaceId);

                foreach (var rollbackException in rollbackExceptions)
                {
                    _logger.LogError(
                        rollbackException,
                        "Failed to roll back a file operation in workspace {WorkspaceId}.",
                        workspaceId);
                }

                throw new AggregateException(
                    "Patch application failed and one or more file operations could not be rolled back.",
                    new[] { applyException }.Concat(rollbackExceptions));
            }

            throw;
        }
        finally
        {
            foreach (var temporaryPath in temporaryFiles)
            {
                if (File.Exists(temporaryPath))
                {
                    File.Delete(temporaryPath);
                }
            }
        }

        return new PatchResult(
            workspaceId,
            true,
            validated.Select(modification =>
                    new FileModificationResult(
                        modification.Request.Path,
                        modification.Request.Operation,
                        modification.Request.Operation == FileOperation.Delete
                            ? null
                            : Hash(modification.ContentBytes!)))
                .ToArray());
    }

    private static async Task<IReadOnlyList<Exception>> RollbackAsync(
        IReadOnlyList<ValidatedModification> applied,
        string workspaceDirectory)
    {
        var failures = new List<Exception>();

        foreach (var modification in applied
                     .Where(modification => modification.Applied)
                     .Reverse())
        {
            try
            {
                if (modification.OriginalBytes is null)
                {
                    if (File.Exists(modification.TargetPath))
                    {
                        File.Delete(modification.TargetPath);
                    }
                }
                else
                {
                    var parentDirectory =
                        Path.GetDirectoryName(modification.TargetPath)
                        ?? throw new InvalidOperationException(
                            "The target file has no parent directory.");
                    Directory.CreateDirectory(parentDirectory);
                    var restoredPath = WorkspacePathResolver.Resolve(
                        workspaceDirectory,
                        modification.Request.Path);
                    var temporaryPath = Path.Combine(
                        parentDirectory,
                        $".copilot-rollback-{Guid.NewGuid():N}.tmp");
                    var temporaryCreated = false;

                    try
                    {
                        await using (var stream = new FileStream(
                                         temporaryPath,
                                         FileMode.CreateNew,
                                         FileAccess.Write,
                                         FileShare.None,
                                         bufferSize: 81920,
                                         FileOptions.Asynchronous))
                        {
                            temporaryCreated = true;
                            await stream.WriteAsync(
                                modification.OriginalBytes,
                                CancellationToken.None);
                            await stream.FlushAsync(CancellationToken.None);
                        }

                        File.Move(
                            temporaryPath,
                            restoredPath,
                            overwrite: true);
                    }
                    finally
                    {
                        if (temporaryCreated &&
                            File.Exists(temporaryPath))
                        {
                            File.Delete(temporaryPath);
                        }
                    }
                }
            }
            catch (Exception rollbackException)
            {
                failures.Add(rollbackException);
            }
        }

        return failures;
    }

    private static string Hash(byte[] bytes)
    {
        return Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
    }

    private static async Task<byte[]> ReadBoundedFileAsync(
        string path,
        long maximumBytes,
        CancellationToken cancellationToken)
    {
        await using var stream = new FileStream(
            path,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            bufferSize: 81920,
            FileOptions.Asynchronous | FileOptions.SequentialScan);

        if (stream.Length > maximumBytes ||
            stream.Length > int.MaxValue)
        {
            throw new ArgumentException(
                $"The original file exceeds the configured patch limit of {maximumBytes} bytes.");
        }

        using var contents = new MemoryStream((int)stream.Length);
        var buffer = new byte[81920];

        while (true)
        {
            var bytesRead = await stream.ReadAsync(
                buffer,
                cancellationToken);

            if (bytesRead == 0)
            {
                break;
            }

            if (contents.Length + bytesRead > maximumBytes ||
                contents.Length + bytesRead > int.MaxValue)
            {
                throw new ArgumentException(
                    $"The original file exceeds the configured patch limit of {maximumBytes} bytes.");
            }

            await contents.WriteAsync(
                buffer.AsMemory(0, bytesRead),
                cancellationToken);
        }

        return contents.ToArray();
    }

    private static async Task<bool> MatchesBaselineAsync(
        WorkspaceModel workspace,
        string relativePath,
        byte[]? currentContent,
        CancellationToken cancellationToken)
    {
        if (currentContent is null)
        {
            return false;
        }

        var baselinePath = WorkspacePathResolver.Resolve(
            WorkspaceManager.GetBaselineDirectory(workspace),
            relativePath);

        if (!File.Exists(baselinePath))
        {
            return false;
        }

        if (new FileInfo(baselinePath).Length != currentContent.LongLength)
        {
            return false;
        }

        var baselineContent = await ReadBoundedFileAsync(
            baselinePath,
            currentContent.LongLength,
            cancellationToken);

        return Hash(baselineContent).Equals(
            Hash(currentContent),
            StringComparison.Ordinal);
    }

    private static bool IsSha256Hash(string value)
    {
        return value.Length == 64 &&
            value.All(Uri.IsHexDigit);
    }

    private sealed class ValidatedModification
    {
        public ValidatedModification(
            FileModification request,
            string targetPath,
            byte[]? originalBytes,
            byte[]? contentBytes)
        {
            Request = request;
            TargetPath = targetPath;
            OriginalBytes = originalBytes;
            ContentBytes = contentBytes;
        }

        public FileModification Request { get; }

        public string TargetPath { get; }

        public byte[]? OriginalBytes { get; }

        public byte[]? ContentBytes { get; }

        public bool Applied { get; set; }
    }
}
