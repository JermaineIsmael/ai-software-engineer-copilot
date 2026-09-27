using Copilot.Api.Models.Repository;

namespace Copilot.Api.Services.Search;

public interface ISearchDocumentIndexingService
{
    Task IndexAsync(
        IReadOnlyList<CodeChunkEmbedding> embeddings,
        CancellationToken cancellationToken = default);
}
