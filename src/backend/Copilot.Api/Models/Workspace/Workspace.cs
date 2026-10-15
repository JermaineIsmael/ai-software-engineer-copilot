namespace Copilot.Api.Models.Workspace;

/// <summary>
/// Describes an isolated working directory used for proposed engineering changes.
/// This model does not create directories or execute Git operations.
/// </summary>
public sealed record Workspace(
    Guid Id,
    WorkspaceRepository Repository,
    WorkspaceBranch Branch,
    string BaseCommitSha,
    string WorkingDirectory,
    DateTimeOffset CreatedAt,
    WorkspaceStatus Status)
{
    /// <summary>
    /// Optional timestamp for tracking the most recent lifecycle update.
    /// </summary>
    public DateTimeOffset? UpdatedAt { get; init; }
}