using System.Text.Json;
using System.Text.Json.Serialization;
using Copilot.Api.Configuration;
using Copilot.Api.Models.Repository;
using Copilot.Api.Models.Workspace;
using Copilot.Api.Services.Repository;
using Microsoft.Extensions.Options;
using WorkspaceModel = Copilot.Api.Models.Workspace.Workspace;

namespace Copilot.Api.Services.Workspace;

public sealed class WorkspaceManager : IWorkspaceManager
{
    private const string WorkingDirectoryName = "working";
    private const string BaselineDirectoryName = ".copilot-baseline";
    private const string MetadataFileName = ".copilot-workspace.json";

    private static readonly JsonSerializerOptions JsonOptions = CreateJsonOptions();

    private readonly IRepositoryProvider _repositoryProvider;
    private readonly WorkspaceOptions _options;
    private readonly WorkspaceOperationLock _operationLock;
    private readonly ILogger<WorkspaceManager> _logger;
    private readonly string _rootDirectory;

    public WorkspaceManager(
        IRepositoryProvider repositoryProvider,
        IOptions<WorkspaceOptions> options,
        WorkspaceOperationLock operationLock,
        ILogger<WorkspaceManager> logger)
    {
        ArgumentNullException.ThrowIfNull(repositoryProvider);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(operationLock);
        ArgumentNullException.ThrowIfNull(logger);

        _repositoryProvider = repositoryProvider;
        _options = options.Value;
        _operationLock = operationLock;
        _logger = logger;
        _rootDirectory = Path.GetFullPath(_options.GetRootDirectory());
    }

    public async Task<WorkspaceModel> CreateAsync(
        string repositoryUrl,
        string branch,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(repositoryUrl);
        ArgumentException.ThrowIfNullOrWhiteSpace(branch);
        cancellationToken.ThrowIfCancellationRequested();

        var parsedRepository = ParseRepositoryUrl(repositoryUrl);
        var snapshot = await _repositoryProvider.GetRepositoryAsync(
            repositoryUrl,
            branch,
            cancellationToken);

        if (string.IsNullOrWhiteSpace(snapshot.CommitSha))
        {
            throw new InvalidOperationException(
                "The repository provider did not return the source commit SHA.");
        }

        if (!IsCommitSha(snapshot.CommitSha))
        {
            throw new InvalidOperationException(
                "The repository provider returned an invalid source commit SHA.");
        }

        if (snapshot.Files is null)
        {
            throw new InvalidOperationException(
                "The repository provider returned an invalid file collection.");
        }

        if (snapshot.FailedFiles is null ||
            snapshot.FailedFiles.Count > 0)
        {
            throw new InvalidOperationException(
                snapshot.FailedFiles is null
                    ? "The repository provider returned invalid failed-file metadata."
                    : $"The repository snapshot is incomplete; {snapshot.FailedFiles.Count} file(s) could not be downloaded.");
        }

        var maximumFiles = _options.MaximumSnapshotFiles;
        var maximumBytes = _options.MaximumSnapshotBytes;

        if (maximumFiles <= 0 || maximumBytes <= 0)
        {
            throw new InvalidOperationException(
                "Workspace snapshot limits must be greater than zero.");
        }

        if (snapshot.Files.Count > maximumFiles)
        {
            throw new InvalidOperationException(
                $"Repository snapshot exceeds the configured file limit of {maximumFiles}.");
        }

        Directory.CreateDirectory(_rootDirectory);

        var workspaceId = Guid.NewGuid();
        var workspaceRoot =
            WorkspacePathResolver.ResolveWorkspaceDirectory(
                _rootDirectory,
                workspaceId);
        var workingDirectory =
            Path.Combine(workspaceRoot, WorkingDirectoryName);
        var baselineDirectory =
            Path.Combine(workspaceRoot, BaselineDirectoryName);
        var createdAt = DateTimeOffset.UtcNow;
        var repositoryId = Guid.NewGuid();
        var defaultBranch = string.IsNullOrWhiteSpace(snapshot.DefaultBranch)
            ? branch
            : snapshot.DefaultBranch;
        var workspace = new WorkspaceModel(
            workspaceId,
            new WorkspaceRepository(
                repositoryId,
                $"https://github.com/{parsedRepository.Owner}/{parsedRepository.Name}",
                defaultBranch,
                parsedRepository.Name),
            new WorkspaceBranch(
                repositoryId,
                branch,
                snapshot.CommitSha),
            snapshot.CommitSha,
            workingDirectory,
            createdAt,
            WorkspaceStatus.Creating);

        try
        {
            Directory.CreateDirectory(workingDirectory);
            Directory.CreateDirectory(baselineDirectory);

            var comparer = OperatingSystem.IsWindows()
                ? StringComparer.OrdinalIgnoreCase
                : StringComparer.Ordinal;
            var importedPaths = new HashSet<string>(comparer);
            long importedBytes = 0;

            foreach (var file in snapshot.Files)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (file is null || file.Content is null)
                {
                    throw new InvalidOperationException(
                        "The repository snapshot contains an invalid file.");
                }

                if (!importedPaths.Add(file.Path))
                {
                    throw new InvalidOperationException(
                        $"The repository snapshot contains duplicate path '{file.Path}'.");
                }

                importedBytes = checked(
                    importedBytes +
                    System.Text.Encoding.UTF8.GetByteCount(file.Content));

                if (importedBytes > maximumBytes)
                {
                    throw new InvalidOperationException(
                        $"Repository snapshot exceeds the configured size limit of {maximumBytes} bytes.");
                }

                var workingPath =
                    WorkspacePathResolver.Resolve(
                        workingDirectory,
                        file.Path);
                var baselinePath =
                    WorkspacePathResolver.Resolve(
                        baselineDirectory,
                        file.Path);

                await WriteImportedFileAsync(
                    workingDirectory,
                    file.Path,
                    workingPath,
                    file.Content,
                    cancellationToken);
                await WriteImportedFileAsync(
                    baselineDirectory,
                    file.Path,
                    baselinePath,
                    file.Content,
                    cancellationToken);
            }

            workspace = workspace with
            {
                Status = WorkspaceStatus.Ready,
                UpdatedAt = DateTimeOffset.UtcNow
            };

            await SaveAsync(
                workspaceRoot,
                workspace,
                cancellationToken);

            return workspace;
        }
        catch (Exception creationException)
        {
            try
            {
                if (Directory.Exists(workspaceRoot))
                {
                    DeleteDirectoryTreeWithoutFollowingReparsePoints(
                        workspaceRoot);
                }
            }
            catch (Exception cleanupException)
            {
                _logger.LogError(
                    cleanupException,
                    "Failed to remove incomplete workspace {WorkspaceId}.",
                    workspaceId);

                throw new AggregateException(
                    "Workspace creation failed and its partial files could not be removed.",
                    creationException,
                    cleanupException);
            }

            throw;
        }
    }

    public async Task<WorkspaceModel> GetAsync(
        Guid workspaceId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var workspaceRoot =
            WorkspacePathResolver.ResolveWorkspaceDirectory(
                _rootDirectory,
                workspaceId);

        return await LoadAsync(
            workspaceRoot,
            cancellationToken);
    }

    public async Task<WorkspaceLease> AcquireAsync(
        Guid workspaceId,
        CancellationToken cancellationToken = default)
    {
        var workspaceLock = await _operationLock.AcquireAsync(
            workspaceId,
            cancellationToken);

        try
        {
            var workspace = await GetAsync(
                workspaceId,
                cancellationToken);

            if (workspace.Status != WorkspaceStatus.Ready)
            {
                throw new InvalidOperationException(
                    $"Workspace '{workspaceId}' is not ready for operations.");
            }

            var activeWorkspace = workspace with
            {
                Status = WorkspaceStatus.InUse,
                UpdatedAt = DateTimeOffset.UtcNow
            };

            var workspaceRoot =
                WorkspacePathResolver.ResolveWorkspaceDirectory(
                    _rootDirectory,
                    workspaceId);

            await SaveAsync(
                workspaceRoot,
                activeWorkspace,
                cancellationToken);

            return new WorkspaceLease(
                activeWorkspace,
                async () =>
                {
                    try
                    {
                        if (Directory.Exists(workspaceRoot))
                        {
                            var readyWorkspace = activeWorkspace with
                            {
                                Status = WorkspaceStatus.Ready,
                                UpdatedAt = DateTimeOffset.UtcNow
                            };

                            await SaveAsync(
                                workspaceRoot,
                                readyWorkspace,
                                CancellationToken.None);
                        }
                    }
                    finally
                    {
                        workspaceLock.Dispose();
                    }
                });
        }
        catch
        {
            workspaceLock.Dispose();
            throw;
        }
    }

    public async Task<WorkspaceModel> CleanupAsync(
        Guid workspaceId,
        CancellationToken cancellationToken = default)
    {
        var workspaceLock = await _operationLock.AcquireAsync(
            workspaceId,
            cancellationToken);

        try
        {
            var workspace = await GetAsync(
                workspaceId,
                cancellationToken);

            if (workspace.Status is not (
                    WorkspaceStatus.Ready or
                    WorkspaceStatus.Failed))
            {
                throw new InvalidOperationException(
                    $"Workspace '{workspaceId}' cannot be cleaned up from state '{workspace.Status}'.");
            }

            var workspaceRoot =
                WorkspacePathResolver.ResolveWorkspaceDirectory(
                    _rootDirectory,
                    workspaceId);
            var cleaningWorkspace = workspace with
            {
                Status = WorkspaceStatus.Cleaning,
                UpdatedAt = DateTimeOffset.UtcNow
            };

            await SaveAsync(
                workspaceRoot,
                cleaningWorkspace,
                cancellationToken);

            try
            {
                DeleteDirectoryTreeWithoutFollowingReparsePoints(
                    workspaceRoot);
            }
            catch (Exception cleanupException)
            {
                _logger.LogError(
                    cleanupException,
                    "Failed to clean workspace {WorkspaceId}.",
                    workspaceId);

                try
                {
                    await SaveAsync(
                        workspaceRoot,
                        cleaningWorkspace with
                        {
                            Status = WorkspaceStatus.Failed,
                            UpdatedAt = DateTimeOffset.UtcNow
                        },
                        CancellationToken.None);
                }
                catch (Exception metadataException)
                {
                    throw new AggregateException(
                        "Workspace cleanup failed and the failure state could not be recorded.",
                        cleanupException,
                        metadataException);
                }

                throw;
            }

            return cleaningWorkspace with
            {
                Status = WorkspaceStatus.Cleaned,
                UpdatedAt = DateTimeOffset.UtcNow
            };
        }
        finally
        {
            workspaceLock.Dispose();
        }
    }

    internal static string GetBaselineDirectory(WorkspaceModel workspace)
    {
        return Path.Combine(
            Path.GetDirectoryName(workspace.WorkingDirectory)
                ?? throw new InvalidOperationException(
                    "Workspace working directory has no parent directory."),
            BaselineDirectoryName);
    }

    private async Task<WorkspaceModel> LoadAsync(
        string workspaceRoot,
        CancellationToken cancellationToken)
    {
        var metadataPath = Path.Combine(
            workspaceRoot,
            MetadataFileName);

        if (!File.Exists(metadataPath))
        {
            throw new KeyNotFoundException(
                "The requested workspace does not exist.");
        }

        EnsureExistingDirectoryIsNotReparsePoint(workspaceRoot);

        if ((File.GetAttributes(metadataPath) & FileAttributes.ReparsePoint) != 0)
        {
            throw new InvalidOperationException(
                "Workspace metadata cannot be a symbolic link or reparse point.");
        }

        await using var stream = new FileStream(
            metadataPath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read | FileShare.Delete,
            bufferSize: 4096,
            FileOptions.Asynchronous | FileOptions.SequentialScan);

        var workspace = await JsonSerializer.DeserializeAsync<WorkspaceModel>(
            stream,
            JsonOptions,
            cancellationToken);

        if (workspace is null ||
            workspace.Id == Guid.Empty ||
            workspace.Id.ToString("N") !=
            Path.GetFileName(workspaceRoot))
        {
            throw new InvalidOperationException(
                "Workspace metadata is invalid.");
        }

        var expectedWorkingDirectory =
            Path.GetFullPath(Path.Combine(workspaceRoot, WorkingDirectoryName));

        EnsureExistingDirectoryIsNotReparsePoint(expectedWorkingDirectory);
        EnsureExistingDirectoryIsNotReparsePoint(
            Path.Combine(workspaceRoot, BaselineDirectoryName));

        if (!Path.GetFullPath(workspace.WorkingDirectory).Equals(
                expectedWorkingDirectory,
                OperatingSystem.IsWindows()
                    ? StringComparison.OrdinalIgnoreCase
                    : StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                "Workspace metadata contains an invalid working directory.");
        }

        return workspace;
    }

    private static async Task SaveAsync(
        string workspaceRoot,
        WorkspaceModel workspace,
        CancellationToken cancellationToken)
    {
        var metadataPath = Path.Combine(
            workspaceRoot,
            MetadataFileName);
        var temporaryPath = Path.Combine(
            workspaceRoot,
            $".copilot-workspace-{Guid.NewGuid():N}.tmp");
        var contents = JsonSerializer.SerializeToUtf8Bytes(
            workspace,
            JsonOptions);

        try
        {
            await File.WriteAllBytesAsync(
                temporaryPath,
                contents,
                cancellationToken);

            if (File.Exists(metadataPath))
            {
                File.Replace(
                    temporaryPath,
                    metadataPath,
                    destinationBackupFileName: null,
                    ignoreMetadataErrors: true);
            }
            else
            {
                File.Move(temporaryPath, metadataPath);
            }
        }
        finally
        {
            if (File.Exists(temporaryPath))
            {
                File.Delete(temporaryPath);
            }
        }
    }

    private static void EnsureExistingDirectoryIsNotReparsePoint(
        string path)
    {
        if (!Directory.Exists(path))
        {
            throw new DirectoryNotFoundException(
                "A required workspace directory does not exist.");
        }

        if ((File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0)
        {
            throw new InvalidOperationException(
                "Workspace directories cannot be symbolic links or reparse points.");
        }
    }

    private static async Task WriteImportedFileAsync(
        string rootDirectory,
        string relativePath,
        string path,
        string content,
        CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(
            Path.GetDirectoryName(path)
                ?? throw new InvalidOperationException(
                    "Imported file path has no parent directory."));

        path = WorkspacePathResolver.Resolve(rootDirectory, relativePath);
        var contentBytes = System.Text.Encoding.UTF8.GetBytes(content);

        await using var stream = new FileStream(
            path,
            FileMode.CreateNew,
            FileAccess.Write,
            FileShare.None,
            bufferSize: 81920,
            FileOptions.Asynchronous);

        await stream.WriteAsync(contentBytes, cancellationToken);
        await stream.FlushAsync(cancellationToken);
    }

    private static void DeleteDirectoryTreeWithoutFollowingReparsePoints(
        string directory)
    {
        if ((File.GetAttributes(directory) & FileAttributes.ReparsePoint) != 0)
        {
            throw new IOException(
                "Refusing to recursively remove a workspace root that is a symbolic link or reparse point.");
        }

        foreach (var entry in Directory.EnumerateFileSystemEntries(directory))
        {
            var attributes = File.GetAttributes(entry);

            if ((attributes & FileAttributes.ReparsePoint) != 0)
            {
                if ((attributes & FileAttributes.Directory) != 0)
                {
                    Directory.Delete(entry, recursive: false);
                }
                else
                {
                    File.Delete(entry);
                }

                continue;
            }

            if ((attributes & FileAttributes.Directory) != 0)
            {
                DeleteDirectoryTreeWithoutFollowingReparsePoints(entry);
            }
            else
            {
                File.Delete(entry);
            }
        }

        Directory.Delete(directory, recursive: false);
    }

    private static (string Owner, string Name) ParseRepositoryUrl(
        string repositoryUrl)
    {
        if (!Uri.TryCreate(repositoryUrl, UriKind.Absolute, out var uri) ||
            uri.Scheme != Uri.UriSchemeHttps ||
            !uri.Host.Equals("github.com", StringComparison.OrdinalIgnoreCase) ||
            uri.Port != 443 ||
            !string.IsNullOrEmpty(uri.UserInfo) ||
            !string.IsNullOrEmpty(uri.Query) ||
            !string.IsNullOrEmpty(uri.Fragment))
        {
            throw new ArgumentException(
                "A valid HTTPS GitHub repository URL is required.",
                nameof(repositoryUrl));
        }

        var segments = uri.AbsolutePath
            .Trim('/')
            .Split('/', StringSplitOptions.RemoveEmptyEntries);

        if (segments.Length != 2)
        {
            throw new ArgumentException(
                "The GitHub repository URL must contain only an owner and repository name.",
                nameof(repositoryUrl));
        }

        var repositoryName = segments[1].EndsWith(
            ".git",
            StringComparison.OrdinalIgnoreCase)
            ? segments[1][..^4]
            : segments[1];

        if (string.IsNullOrWhiteSpace(segments[0]) ||
            string.IsNullOrWhiteSpace(repositoryName))
        {
            throw new ArgumentException(
                "The GitHub repository URL must contain an owner and repository name.",
                nameof(repositoryUrl));
        }

        return (segments[0], repositoryName);
    }

    private static bool IsCommitSha(string commitSha)
    {
        return commitSha.Length is >= 40 and <= 64 &&
            commitSha.All(Uri.IsHexDigit);
    }

    private static JsonSerializerOptions CreateJsonOptions()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        options.Converters.Add(new JsonStringEnumConverter());
        return options;
    }
}
