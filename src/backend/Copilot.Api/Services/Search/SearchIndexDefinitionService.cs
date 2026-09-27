using Azure.Search.Documents.Indexes.Models;
using Copilot.Api.Configuration;
using Microsoft.Extensions.Options;

namespace Copilot.Api.Services.Search;

public sealed class SearchIndexDefinitionService : ISearchIndexDefinitionService
{
    private readonly AzureSearchOptions _options;

    public SearchIndexDefinitionService(
        IOptions<AzureSearchOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);

        _options = options.Value;
    }

    public SearchIndex CreateDefinition()
    {
        var fields = new List<SearchField>
        {
            new SearchField("id", SearchFieldDataType.String)
            {
                IsKey = true,
                IsFilterable = true
            },

            new SearchField("repository", SearchFieldDataType.String)
            {
                IsFilterable = true
            },

            new SearchField("branch", SearchFieldDataType.String)
            {
                IsFilterable = true
            },

            new SearchField("path", SearchFieldDataType.String)
            {
                IsSearchable = true,
                IsFilterable = true
            },

            new SearchField("language", SearchFieldDataType.String)
            {
                IsFilterable = true,
                IsFacetable = true
            },

            new SearchField("chunkIndex", SearchFieldDataType.Int32)
            {
                IsFilterable = true
            },

            new SearchField("startLine", SearchFieldDataType.Int32),

            new SearchField("endLine", SearchFieldDataType.Int32),

            new SearchField("content", SearchFieldDataType.String)
            {
                IsSearchable = true
            },

            new SearchField(
                "contentVector",
                SearchFieldDataType.Collection(
                    SearchFieldDataType.Single))
            {
                IsSearchable = true,
                VectorSearchDimensions = _options.VectorDimensions,
                VectorSearchProfileName = "code-vector-profile"
            }
        };

        var vectorSearch = new VectorSearch
        {
            Profiles =
            {
                new VectorSearchProfile(
                    "code-vector-profile",
                    "code-hnsw")
            },
            Algorithms =
            {
                new HnswAlgorithmConfiguration(
                    "code-hnsw")
            }
        };

        return new SearchIndex(
            _options.IndexName)
        {
            Fields = fields,
            VectorSearch = vectorSearch
        };
    }
}
