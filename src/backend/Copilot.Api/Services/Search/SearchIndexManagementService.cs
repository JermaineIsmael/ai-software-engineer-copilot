using Azure;
using Azure.Search.Documents.Indexes;
using Copilot.Api.Configuration;
using Microsoft.Extensions.Options;

namespace Copilot.Api.Services.Search;

public sealed class SearchIndexManagementService : ISearchIndexManagementService
{
    private readonly SearchIndexClient _indexClient;
    private readonly ISearchIndexDefinitionService _definitionService;

    public SearchIndexManagementService(
        ISearchIndexDefinitionService definitionService,
        IOptions<AzureSearchOptions> options)
    {
        ArgumentNullException.ThrowIfNull(definitionService);
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

        _definitionService = definitionService;

        _indexClient = new SearchIndexClient(
            new Uri(searchOptions.Endpoint),
            new AzureKeyCredential(searchOptions.ApiKey));
    }

    public async Task EnsureIndexAsync(
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var definition = _definitionService.CreateDefinition();

        await _indexClient.CreateOrUpdateIndexAsync(
            definition,
            cancellationToken: cancellationToken);
    }
}



