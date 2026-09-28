using Copilot.Api.Models.Search;

namespace Copilot.Api.Services.Search;

public interface IHybridSearchService
{
    Task<IReadOnlyList<HybridSearchResult>> SearchAsync(
        string query,
        IReadOnlyList<float> queryEmbedding,
        int top,
        string repository,
        string branch,
        CancellationToken cancellationToken = default);
}
