#pragma warning disable OPENAI001

using Copilot.Api.Models.Repository;
using OpenAI;
using OpenAI.Embeddings;
using System.ClientModel;

namespace Copilot.Api.Services.Repository;

public sealed class AzureOpenAIEmbeddingService : IEmbeddingService
{
    private readonly EmbeddingClient _client;

    public AzureOpenAIEmbeddingService(
        IConfiguration configuration)
    {
        var endpoint = configuration["AzureOpenAI:Endpoint"];
        var apiKey = configuration["AzureOpenAI:ApiKey"];

        var deploymentName =
            configuration["AzureOpenAI:EmbeddingDeploymentName"];

        if (string.IsNullOrWhiteSpace(endpoint))
        {
            throw new InvalidOperationException(
                "Azure OpenAI endpoint is not configured.");
        }

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new InvalidOperationException(
                "Azure OpenAI API key is not configured.");
        }

        if (string.IsNullOrWhiteSpace(deploymentName))
        {
            throw new InvalidOperationException(
                "Azure OpenAI embedding deployment name is not configured.");
        }

        var options = new OpenAIClientOptions
        {
            Endpoint = new Uri(
                $"{endpoint.TrimEnd('/')}/openai/v1/")
        };

        _client = new EmbeddingClient(
            deploymentName,
            new ApiKeyCredential(apiKey),
            options);
    }

    public async Task<IReadOnlyList<CodeChunkEmbedding>> GenerateEmbeddingsAsync(
        IReadOnlyList<CodeChunk> chunks,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(chunks);

        if (chunks.Count == 0)
        {
            return Array.Empty<CodeChunkEmbedding>();
        }

        var results = new List<CodeChunkEmbedding>(
            chunks.Count);

        foreach (var chunk in chunks)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (string.IsNullOrWhiteSpace(chunk.Content))
            {
                throw new ArgumentException(
                    $"Chunk {chunk.ChunkIndex} for '{chunk.Path}' has no content.",
                    nameof(chunks));
            }

            var embedding = await _client.GenerateEmbeddingAsync(
                chunk.Content,
                cancellationToken: cancellationToken);

            var vector = embedding.Value.ToFloats();

            results.Add(
                new CodeChunkEmbedding(
                    chunk.Repository,
                    chunk.Branch,
                    chunk.Path,
                    chunk.Language,
                    chunk.ChunkIndex,
                    chunk.StartLine,
                    chunk.EndLine,
                    chunk.Content,
                    vector.ToArray()));
        }

        return results;
    }
}
