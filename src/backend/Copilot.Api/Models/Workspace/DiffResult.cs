namespace Copilot.Api.Models.Workspace;

public sealed record DiffResult(
    Guid WorkspaceId,
    IReadOnlyList<FileDiff> Files)
{
    public bool HasChanges => Files.Count > 0;
}
