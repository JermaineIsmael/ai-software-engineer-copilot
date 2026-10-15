namespace Copilot.Api.Models.Workspace;

/// <summary>
/// Identifies a repository branch and the commit used as its baseline.
/// </summary>
public sealed record WorkspaceBranch(
    Guid RepositoryId,
    string Name,
    string CommitSha);