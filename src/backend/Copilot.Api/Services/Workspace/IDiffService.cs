using Copilot.Api.Models.Workspace;

namespace Copilot.Api.Services.Workspace;

public interface IDiffService
{
    Task<DiffResult> GetDiffAsync(
        Guid workspaceId,
        CancellationToken cancellationToken = default);
}
