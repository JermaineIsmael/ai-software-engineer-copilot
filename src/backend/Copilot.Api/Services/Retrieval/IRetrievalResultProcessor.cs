using Copilot.Api.Models.Search;

namespace Copilot.Api.Services.Retrieval;

public interface IRetrievalResultProcessor
{
    IReadOnlyList<HybridSearchResult> Process(
        IReadOnlyList<HybridSearchResult> results,
        int top);
}
