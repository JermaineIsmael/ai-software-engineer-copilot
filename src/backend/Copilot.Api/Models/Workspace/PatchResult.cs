namespace Copilot.Api.Models.Workspace;

public sealed record PatchResult(
    Guid WorkspaceId,
    bool Success,
    IReadOnlyList<FileModificationResult> Files);
