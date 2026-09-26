using Copilot.Api.Models.Repository;

namespace Copilot.Api.Services.Repository;

public interface IEmbeddingService
{
    Task<IReadOnlyList<CodeChunkEmbedding>> GenerateEmbeddingsAsync(
        IReadOnlyList<CodeChunk> chunks,
        CancellationToken cancellationToken = default);
}