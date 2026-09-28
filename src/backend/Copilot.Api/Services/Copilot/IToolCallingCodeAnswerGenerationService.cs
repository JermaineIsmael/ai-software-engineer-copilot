using Copilot.Api.Services.Tools;

namespace Copilot.Api.Services.Copilot;

public interface IToolCallingCodeAnswerGenerationService
{
    Task<string> GenerateAsync(
        string query,
        string repository,
        string branch,
        IReadOnlyCollection<ICopilotTool> tools,
        CancellationToken cancellationToken = default);
}
