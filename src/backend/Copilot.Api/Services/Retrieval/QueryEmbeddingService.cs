using Copilot.Api.Models.Repository;
using Copilot.Api.Services.Repository;

namespace Copilot.Api.Services.Retrieval;

public sealed class QueryEmbeddingService : IQueryEmbeddingService
{
    private readonly IEmbeddingService _embeddingService;

    public QueryEmbeddingService(
        IEmbeddingService embeddingService)
    {
        ArgumentNullException.ThrowIfNull(embeddingService);

        _embeddingService = embeddingService;
    }

    public async Task<IReadOnlyList<float>> GenerateAsync(
        string query,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            throw new ArgumentException(
                "Query cannot be empty.",
                nameof(query));
        }

        cancellationToken.ThrowIfCancellationRequested();

        var queryChunk = new CodeChunk(
            Repository: string.Empty,
            Branch: string.Empty,
            Path: string.Empty,
            Language: "text",
            ChunkIndex: 0,
            StartLine: 0,
            EndLine: 0,
            Content: query);

        var embeddings = await _embeddingService.GenerateEmbeddingsAsync(
            new[] { queryChunk },
            cancellationToken);

        if (embeddings.Count != 1)
        {
            throw new InvalidOperationException(
                "The embedding service must return exactly one embedding for a query.");
        }

        return embeddings[0].Embedding;
    }
}
