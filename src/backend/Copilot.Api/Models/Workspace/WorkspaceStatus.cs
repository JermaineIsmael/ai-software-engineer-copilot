namespace Copilot.Api.Models.Workspace;

/// <summary>
/// Lifecycle states for an agent-managed workspace.
/// </summary>
public enum WorkspaceStatus
{
    Creating,
    Ready,
    InUse,
    Completed,
    Failed,
    Cleaning,
    Cleaned
}