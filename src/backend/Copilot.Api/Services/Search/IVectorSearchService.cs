using Copilot.Api.Models.Search;

namespace Copilot.Api.Services.Search;

public interface IVectorSearchService
{
    Task<IReadOnlyList<VectorSearchResult>> SearchAsync(
        IReadOnlyList<float> queryEmbedding,
        int top = 5,
        CancellationToken cancellationToken = default);
}
