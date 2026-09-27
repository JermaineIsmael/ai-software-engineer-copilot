using Copilot.Api.Models.Search;
using Copilot.Api.Services.Retrieval;

namespace Copilot.Api.Tests.Retrieval;

internal sealed class FakeRetrievalResultProcessor : IRetrievalResultProcessor
{
    public IReadOnlyList<HybridSearchResult> Process(
        IReadOnlyList<HybridSearchResult> results,
        int top)
    {
        return results.Take(top).ToList();
    }
}
