namespace Copilot.Api.Models.Workspace;

/// <summary>
/// Identifies the source repository from which an isolated workspace is created.
/// </summary>
public sealed record WorkspaceRepository(
    Guid Id,
    string Url,
    string DefaultBranch,
    string? Name = null);