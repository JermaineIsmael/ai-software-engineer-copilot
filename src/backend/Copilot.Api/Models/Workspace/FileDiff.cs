namespace Copilot.Api.Models.Workspace;

public sealed record FileDiff(
    string Path,
    FileOperation Operation,
    string UnifiedDiff,
    string? OriginalContentHash,
    string? ModifiedContentHash);
