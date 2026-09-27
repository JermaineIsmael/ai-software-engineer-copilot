namespace Copilot.Api.Services.Retrieval;

public interface IQueryEmbeddingService
{
    Task<IReadOnlyList<float>> GenerateAsync(
        string query,
        CancellationToken cancellationToken = default);
}
