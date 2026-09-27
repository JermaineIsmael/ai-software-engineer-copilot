using Copilot.Api.Models.Retrieval;

namespace Copilot.Api.Services.Retrieval;

public interface ICodeRetrievalService
{
    Task<CodeRetrievalResponse> RetrieveAsync(
        CodeRetrievalRequest request,
        CancellationToken cancellationToken = default);
}
