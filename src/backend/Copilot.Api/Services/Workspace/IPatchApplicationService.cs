using Copilot.Api.Models.Workspace;

namespace Copilot.Api.Services.Workspace;

public interface IPatchApplicationService
{
    Task<PatchResult> ApplyAsync(
        Guid workspaceId,
        Patch patch,
        CancellationToken cancellationToken = default);
}
