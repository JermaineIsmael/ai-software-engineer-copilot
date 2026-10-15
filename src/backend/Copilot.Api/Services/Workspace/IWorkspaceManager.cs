using Copilot.Api.Models.Workspace;
using WorkspaceModel = Copilot.Api.Models.Workspace.Workspace;

namespace Copilot.Api.Services.Workspace;

public interface IWorkspaceManager
{
    Task<WorkspaceModel> CreateAsync(
        string repositoryUrl,
        string branch,
        CancellationToken cancellationToken = default);

    Task<WorkspaceModel> GetAsync(
        Guid workspaceId,
        CancellationToken cancellationToken = default);

    Task<WorkspaceLease> AcquireAsync(
        Guid workspaceId,
        CancellationToken cancellationToken = default);

    Task<WorkspaceModel> CleanupAsync(
        Guid workspaceId,
        CancellationToken cancellationToken = default);
}
