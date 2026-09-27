using Copilot.Api.Models.Retrieval;
using Copilot.Api.Services.Retrieval;

namespace Copilot.Api.Tests.Copilot;

internal sealed class FakeCodeRetrievalService
    : ICodeRetrievalService
{
    public CodeRetrievalResponse Response { get; set; } =
        new(
            "test query",
            Array.Empty<RetrievedCodeChunk>());

    public Task<CodeRetrievalResponse> RetrieveAsync(
        CodeRetrievalRequest request,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(
            Response with
            {
                Query = request.Query
            });
    }
}
