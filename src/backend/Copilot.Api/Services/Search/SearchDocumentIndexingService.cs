using Azure;
using Azure.Search.Documents;
using Copilot.Api.Configuration;
using Copilot.Api.Models.Repository;
using Copilot.Api.Models.Search;
using Microsoft.Extensions.Options;

namespace Copilot.Api.Services.Search;

public sealed class SearchDocumentIndexingService
    : ISearchDocumentIndexingService
{
    private readonly SearchClient _searchClient;

    public SearchDocumentIndexingService(
        IOptions<AzureSearchOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var searchOptions = options.Value;

        if (string.IsNullOrWhiteSpace(searchOptions.Endpoint))
        {
            throw new InvalidOperationException(
                "Azure Search endpoint is not configured.");
        }

        if (string.IsNullOrWhiteSpace(searchOptions.ApiKey))
        {
            throw new InvalidOperationException(
                "Azure Search API key is not configured.");
        }

        if (string.IsNullOrWhiteSpace(searchOptions.IndexName))
        {
            throw new InvalidOperationException(
                "Azure Search index name is not configured.");
        }

        _searchClient = new SearchClient(
            new Uri(searchOptions.Endpoint),
            searchOptions.IndexName,
            new AzureKeyCredential(searchOptions.ApiKey));
    }

    public async Task IndexAsync(
        IReadOnlyList<CodeChunkEmbedding> embeddings,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(embeddings);

        if (embeddings.Count == 0)
        {
            return;
        }

        cancellationToken.ThrowIfCancellationRequested();

        var documents = embeddings
            .Select(CreateDocument)
            .ToList();

        await _searchClient.UploadDocumentsAsync(
            documents,
            cancellationToken: cancellationToken);
    }

    private static CodeChunkSearchDocument CreateDocument(
        CodeChunkEmbedding embedding)
    {
        ArgumentNullException.ThrowIfNull(embedding);

        return new CodeChunkSearchDocument(
            CreateDocumentId(embedding),
            embedding.Repository,
            embedding.Branch,
            embedding.Path,
            embedding.Language,
            embedding.ChunkIndex,
            embedding.StartLine,
            embedding.EndLine,
            embedding.Content,
            embedding.Embedding);
    }

    private static string CreateDocumentId(
        CodeChunkEmbedding embedding)
    {
        var rawId =
            $"{embedding.Repository}|{embedding.Branch}|{embedding.Path}|{embedding.ChunkIndex}";

        return Convert.ToHexString(
            System.Security.Cryptography.SHA256.HashData(
                System.Text.Encoding.UTF8.GetBytes(rawId)));
    }
}
