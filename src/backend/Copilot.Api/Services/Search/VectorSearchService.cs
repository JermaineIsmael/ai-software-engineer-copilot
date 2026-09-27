using Azure;
using Azure.Search.Documents;
using Azure.Search.Documents.Models;
using Copilot.Api.Configuration;
using Copilot.Api.Models.Search;
using Microsoft.Extensions.Options;

namespace Copilot.Api.Services.Search;

public sealed class VectorSearchService : IVectorSearchService
{
    private readonly SearchClient _searchClient;

    public VectorSearchService(
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

    public async Task<IReadOnlyList<VectorSearchResult>> SearchAsync(
        IReadOnlyList<float> queryEmbedding,
        int top = 5,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(queryEmbedding);

        if (queryEmbedding.Count == 0)
        {
            throw new ArgumentException(
                "Query embedding cannot be empty.",
                nameof(queryEmbedding));
        }

        if (top <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(top),
                "Top must be greater than zero.");
        }

        cancellationToken.ThrowIfCancellationRequested();

        var vectorQuery = new VectorizedQuery(
            queryEmbedding.ToArray())
        {
            KNearestNeighborsCount = top,
            Fields = { "contentVector" }
        };

        var searchOptions = new SearchOptions
        {
            Size = top
        };

        searchOptions.VectorSearch.Queries.Add(vectorQuery);

        var response = await _searchClient.SearchAsync<SearchDocument>(
            null,
            searchOptions,
            cancellationToken);

        var results = new List<VectorSearchResult>();

        await foreach (var result in response.Value.GetResultsAsync())
        {
            var document = result.Document;

            results.Add(
                new VectorSearchResult(
                    GetString(document, "id"),
                    GetString(document, "repository"),
                    GetString(document, "branch"),
                    GetString(document, "path"),
                    GetString(document, "language"),
                    GetInt32(document, "chunkIndex"),
                    GetInt32(document, "startLine"),
                    GetInt32(document, "endLine"),
                    GetString(document, "content"),
                    result.Score ?? 0));
        }

        return results;
    }

    private static string GetString(
        SearchDocument document,
        string field)
    {
        return document.TryGetValue(field, out var value)
            ? value?.ToString() ?? string.Empty
            : string.Empty;
    }

    private static int GetInt32(
        SearchDocument document,
        string field)
    {
        if (!document.TryGetValue(field, out var value) ||
            value is null)
        {
            return 0;
        }

        return Convert.ToInt32(value);
    }
}
