using System.Security.Cryptography;
using System.Text;
using Copilot.Api.Models.Workspace;

namespace Copilot.Api.Services.Workspace;

public sealed class DiffService : IDiffService
{
    private static readonly UTF8Encoding StrictUtf8 = new(
        encoderShouldEmitUTF8Identifier: false,
        throwOnInvalidBytes: true);

    private readonly IWorkspaceManager _workspaceManager;

    public DiffService(IWorkspaceManager workspaceManager)
    {
        ArgumentNullException.ThrowIfNull(workspaceManager);
        _workspaceManager = workspaceManager;
    }

    public async Task<DiffResult> GetDiffAsync(
        Guid workspaceId,
        CancellationToken cancellationToken = default)
    {
        await using var lease = await _workspaceManager.AcquireAsync(
            workspaceId,
            cancellationToken);

        var workspace = lease.Workspace;
        var baselineDirectory =
            WorkspaceManager.GetBaselineDirectory(workspace);
        var baselineFiles = EnumerateFiles(
            baselineDirectory,
            cancellationToken);
        var workingFiles = EnumerateFiles(
            workspace.WorkingDirectory,
            cancellationToken);
        var comparer = OperatingSystem.IsWindows()
            ? StringComparer.OrdinalIgnoreCase
            : StringComparer.Ordinal;
        var paths = baselineFiles.Keys
            .Union(workingFiles.Keys, comparer)
            .OrderBy(path => path, comparer);
        var diffs = new List<FileDiff>();

        foreach (var path in paths)
        {
            cancellationToken.ThrowIfCancellationRequested();

            baselineFiles.TryGetValue(path, out var baselinePath);
            workingFiles.TryGetValue(path, out var workingPath);

            var originalBytes = baselinePath is null
                ? Array.Empty<byte>()
                : await File.ReadAllBytesAsync(
                    baselinePath,
                    cancellationToken);
            var modifiedBytes = workingPath is null
                ? Array.Empty<byte>()
                : await File.ReadAllBytesAsync(
                    workingPath,
                    cancellationToken);

            if (baselinePath is not null &&
                workingPath is not null &&
                originalBytes.AsSpan().SequenceEqual(modifiedBytes))
            {
                continue;
            }

            var operation = baselinePath is null
                ? FileOperation.Create
                : workingPath is null
                    ? FileOperation.Delete
                    : FileOperation.Update;
            var originalContent = Decode(originalBytes, path);
            var modifiedContent = Decode(modifiedBytes, path);

            diffs.Add(
                new FileDiff(
                    path,
                    operation,
                    CreateUnifiedDiff(
                        path,
                        originalContent,
                        modifiedContent),
                    baselinePath is null ? null : Hash(originalBytes),
                    workingPath is null ? null : Hash(modifiedBytes)));
        }

        return new DiffResult(workspaceId, diffs);
    }

    private static Dictionary<string, string> EnumerateFiles(
        string root,
        CancellationToken cancellationToken)
    {
        if (!Directory.Exists(root))
        {
            throw new InvalidOperationException(
                "The workspace baseline or working directory is missing.");
        }

        if ((File.GetAttributes(root) & FileAttributes.ReparsePoint) != 0)
        {
            throw new InvalidOperationException(
                "Workspace diffs cannot read a symbolic link or reparse-point root.");
        }

        var comparer = OperatingSystem.IsWindows()
            ? StringComparer.OrdinalIgnoreCase
            : StringComparer.Ordinal;
        var files = new Dictionary<string, string>(comparer);
        var directories = new Stack<string>();
        directories.Push(root);

        while (directories.Count > 0)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var currentDirectory = directories.Pop();

            foreach (var entry in Directory.EnumerateFileSystemEntries(
                         currentDirectory))
            {
                cancellationToken.ThrowIfCancellationRequested();

                var attributes = File.GetAttributes(entry);

                if ((attributes & FileAttributes.ReparsePoint) != 0)
                {
                    throw new InvalidOperationException(
                        "Workspace diffs cannot traverse symbolic links or reparse points.");
                }

                if ((attributes & FileAttributes.Directory) != 0)
                {
                    directories.Push(entry);
                    continue;
                }

                var relativePath = Path.GetRelativePath(root, entry)
                    .Replace(Path.DirectorySeparatorChar, '/');

                _ = WorkspacePathResolver.Resolve(root, relativePath);

                if (!files.TryAdd(relativePath, entry))
                {
                    throw new InvalidOperationException(
                        $"Workspace contains duplicate file path '{relativePath}'.");
                }
            }
        }

        return files;
    }

    private static string Decode(byte[] bytes, string path)
    {
        try
        {
            return StrictUtf8.GetString(bytes);
        }
        catch (DecoderFallbackException exception)
        {
            throw new InvalidOperationException(
                $"Cannot generate a text diff for non-UTF-8 file '{path}'.",
                exception);
        }
    }

    private static string CreateUnifiedDiff(
        string path,
        string original,
        string modified)
    {
        var oldLines = SplitLines(original);
        var newLines = SplitLines(modified);
        var finalNewlineDiffers =
            oldLines.HasFinalNewline != newLines.HasFinalNewline;
        var commonPrefix = 0;

        while (commonPrefix < oldLines.Lines.Length &&
               commonPrefix < newLines.Lines.Length &&
               oldLines.Lines[commonPrefix] == newLines.Lines[commonPrefix])
        {
            commonPrefix++;
        }

        if (finalNewlineDiffers &&
            oldLines.Lines.SequenceEqual(newLines.Lines) &&
            commonPrefix > 0)
        {
            commonPrefix--;
        }

        var commonSuffix = 0;
        var maximumCommonSuffix = Math.Min(
            oldLines.Lines.Length - commonPrefix,
            newLines.Lines.Length - commonPrefix);

        if (finalNewlineDiffers &&
            oldLines.Lines.Length > 0 &&
            newLines.Lines.Length > 0)
        {
            maximumCommonSuffix = Math.Max(0, maximumCommonSuffix - 1);
        }

        while (commonSuffix < maximumCommonSuffix &&
               oldLines.Lines[oldLines.Lines.Length - commonSuffix - 1] ==
               newLines.Lines[newLines.Lines.Length - commonSuffix - 1])
        {
            commonSuffix++;
        }

        var prefixStart = Math.Max(0, commonPrefix - 3);
        var oldChangeEnd = oldLines.Lines.Length - commonSuffix;
        var newChangeEnd = newLines.Lines.Length - commonSuffix;
        var oldEnd = Math.Min(oldLines.Lines.Length, oldChangeEnd + 3);
        var newEnd = Math.Min(newLines.Lines.Length, newChangeEnd + 3);
        var oldCount = oldEnd - prefixStart;
        var newCount = newEnd - prefixStart;
        var builder = new StringBuilder();

        builder.Append("--- a/").Append(path).Append('\n');
        builder.Append("+++ b/").Append(path).Append('\n');
        builder.Append("@@ -")
            .Append(oldCount == 0 ? 0 : prefixStart + 1)
            .Append(',')
            .Append(oldCount)
            .Append(" +")
            .Append(newCount == 0 ? 0 : prefixStart + 1)
            .Append(',')
            .Append(newCount)
            .Append(" @@\n");

        for (var index = prefixStart; index < commonPrefix; index++)
        {
            AppendLine(builder, ' ', oldLines, index);
        }

        for (var index = commonPrefix; index < oldChangeEnd; index++)
        {
            AppendLine(builder, '-', oldLines, index);
        }

        for (var index = commonPrefix; index < newChangeEnd; index++)
        {
            AppendLine(builder, '+', newLines, index);
        }

        for (var index = oldChangeEnd; index < oldEnd; index++)
        {
            AppendLine(builder, ' ', oldLines, index);
        }

        return builder.ToString();
    }

    private static SplitFileLines SplitLines(string content)
    {
        var hasFinalNewline = content.EndsWith('\n');

        if (content.Length == 0)
        {
            return new SplitFileLines(Array.Empty<string>(), false);
        }

        var lines = content.Split('\n');

        if (hasFinalNewline)
        {
            Array.Resize(ref lines, lines.Length - 1);
        }

        for (var index = 0; index < lines.Length; index++)
        {
            if (lines[index].EndsWith('\r'))
            {
                lines[index] = lines[index][..^1];
            }
        }

        return new SplitFileLines(lines, hasFinalNewline);
    }

    private static void AppendLine(
        StringBuilder builder,
        char prefix,
        SplitFileLines file,
        int index)
    {
        builder.Append(prefix).Append(file.Lines[index]).Append('\n');

        if (index == file.Lines.Length - 1 &&
            !file.HasFinalNewline)
        {
            builder.Append("\\ No newline at end of file\n");
        }
    }

    private static string Hash(byte[] bytes)
    {
        return Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
    }

    private sealed record SplitFileLines(
        string[] Lines,
        bool HasFinalNewline);
}
