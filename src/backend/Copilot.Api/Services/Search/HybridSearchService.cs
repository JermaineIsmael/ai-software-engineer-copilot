using Azure;
using Azure.Search.Documents;
using Azure.Search.Documents.Models;
using Copilot.Api.Configuration;
using Copilot.Api.Models.Search;
using Microsoft.Extensions.Options;

namespace Copilot.Api.Services.Search;

public sealed class HybridSearchService : IHybridSearchService
{
    private readonly SearchClient _searchClient;

    public HybridSearchService(
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

    public async Task<IReadOnlyList<HybridSearchResult>> SearchAsync(
        string query,
        IReadOnlyList<float> queryEmbedding,
        int top,
        string repository,
        string branch,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            throw new ArgumentException(
                "Search query cannot be empty.",
                nameof(query));
        }

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

        if (string.IsNullOrWhiteSpace(repository))
        {
            throw new ArgumentException(
                "Repository cannot be empty.",
                nameof(repository));
        }

        if (string.IsNullOrWhiteSpace(branch))
        {
            throw new ArgumentException(
                "Branch cannot be empty.",
                nameof(branch));
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
            Size = top,
            VectorSearch = new VectorSearchOptions(),
            Filter =
                $"repository eq '{EscapeODataValue(repository)}' " +
                $"and branch eq '{EscapeODataValue(branch)}'"
        };

        searchOptions.VectorSearch.Queries.Add(vectorQuery);

        var response = await _searchClient.SearchAsync<SearchDocument>(
            query,
            searchOptions,
            cancellationToken);

        var results = new List<HybridSearchResult>();

        await foreach (var result in response.Value.GetResultsAsync())
        {
            var document = result.Document;

            results.Add(
                new HybridSearchResult(
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

    private static string EscapeODataValue(
        string value)
    {
        return value.Replace("'", "''");
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
