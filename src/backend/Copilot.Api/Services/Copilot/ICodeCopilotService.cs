using Copilot.Api.Models.Copilot;

namespace Copilot.Api.Services.Copilot;

public interface ICodeCopilotService
{
    Task<CopilotResponse> AskAsync(
        CopilotRequest request,
        CancellationToken cancellationToken = default);
}
