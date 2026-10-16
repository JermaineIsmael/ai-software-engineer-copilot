using Copilot.Api.Models.Workspace;

namespace Copilot.Api.Services.Workspace;

public interface IBuildTestExecutionService
{
    Task<BuildTestExecutionResult> RunAsync(
        Guid workspaceId,
        BuildTestOperation operation,
        string targetPath,
        CancellationToken cancellationToken = default);
}
