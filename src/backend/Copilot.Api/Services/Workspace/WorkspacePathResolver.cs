namespace Copilot.Api.Services.Workspace;

internal static class WorkspacePathResolver
{
    private const string MetadataFileName = ".copilot-workspace.json";

    public static string Resolve(
        string workspaceDirectory,
        string relativePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(workspaceDirectory);
        ArgumentException.ThrowIfNullOrWhiteSpace(relativePath);

        if (relativePath.Contains('\\') ||
            relativePath.Contains('\0') ||
            Path.IsPathRooted(relativePath) ||
            relativePath.Contains(':'))
        {
            throw new ArgumentException(
                "File paths must be relative repository paths using '/' separators.",
                nameof(relativePath));
        }

        var segments = relativePath.Split('/');

        if (segments.Any(segment =>
                string.IsNullOrWhiteSpace(segment) ||
                segment is "." or ".." ||
                segment.EndsWith(' ') ||
                segment.EndsWith('.')))
        {
            throw new ArgumentException(
                "File paths cannot contain empty, dot, or parent-directory segments.",
                nameof(relativePath));
        }

        if (relativePath.Equals(
                MetadataFileName,
                StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException(
                "The path is reserved for workspace metadata.",
                nameof(relativePath));
        }

        var root = Path.GetFullPath(workspaceDirectory);
        var resolved = Path.GetFullPath(
            Path.Combine(root, Path.Combine(segments)));
        var rootPrefix = Path.EndsInDirectorySeparator(root)
            ? root
            : root + Path.DirectorySeparatorChar;

        if (!resolved.StartsWith(
                rootPrefix,
                OperatingSystem.IsWindows()
                    ? StringComparison.OrdinalIgnoreCase
                    : StringComparison.Ordinal))
        {
            throw new ArgumentException(
                "The file path is outside the workspace.",
                nameof(relativePath));
        }

        EnsureNoReparsePoint(root, segments, includeFinalPath: true);
        return resolved;
    }

    public static string ResolveWorkspaceDirectory(
        string rootDirectory,
        Guid workspaceId)
    {
        if (workspaceId == Guid.Empty)
        {
            throw new ArgumentException(
                "Workspace ID cannot be empty.",
                nameof(workspaceId));
        }

        var root = Path.GetFullPath(rootDirectory);
        var resolved = Path.GetFullPath(
            Path.Combine(root, workspaceId.ToString("N")));
        var prefix = Path.EndsInDirectorySeparator(root)
            ? root
            : root + Path.DirectorySeparatorChar;

        if (!resolved.StartsWith(
                prefix,
                OperatingSystem.IsWindows()
                    ? StringComparison.OrdinalIgnoreCase
                    : StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                "Workspace directory resolved outside its configured root.");
        }

        return resolved;
    }

    private static void EnsureNoReparsePoint(
        string root,
        IReadOnlyList<string> segments,
        bool includeFinalPath)
    {
        if (Directory.Exists(root) &&
            (File.GetAttributes(root) & FileAttributes.ReparsePoint) != 0)
        {
            throw new ArgumentException(
                "Workspace paths cannot use a symbolic link or reparse-point root.",
                nameof(root));
        }

        var current = root;
        var segmentsToCheck = includeFinalPath
            ? segments.Count
            : Math.Max(0, segments.Count - 1);

        for (var index = 0; index < segmentsToCheck; index++)
        {
            current = Path.Combine(current, segments[index]);

            if (!File.Exists(current) && !Directory.Exists(current))
            {
                continue;
            }

            var attributes = File.GetAttributes(current);

            if ((attributes & FileAttributes.ReparsePoint) != 0)
            {
                throw new ArgumentException(
                    "Workspace paths cannot traverse symbolic links or reparse points.",
                    nameof(segments));
            }
        }
    }
}
