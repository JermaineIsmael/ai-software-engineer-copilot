using Azure.Search.Documents.Indexes.Models;
using Copilot.Api.Configuration;
using Copilot.Api.Services.Search;
using Microsoft.Extensions.Options;

namespace Copilot.Api.Tests.Search;

public sealed class SearchIndexManagementServiceTests
{
    [Fact]
    public void Constructor_WhenDefinitionServiceIsNull_Throws()
    {
        var options = Options.Create(
            new AzureSearchOptions
            {
                Endpoint = "https://example.search.windows.net",
                ApiKey = "test-key",
                IndexName = "code-chunks",
                VectorDimensions = 1536
            });

        var exception = Assert.Throws<ArgumentNullException>(
            () => new SearchIndexManagementService(
                null!,
                options));

        Assert.Equal("definitionService", exception.ParamName);
    }

    [Fact]
    public void Constructor_WhenOptionsAreNull_Throws()
    {
        var definitionService =
            new FakeSearchIndexDefinitionService();

        var exception = Assert.Throws<ArgumentNullException>(
            () => new SearchIndexManagementService(
                definitionService,
                null!));

        Assert.Equal("options", exception.ParamName);
    }

    [Fact]
    public void Constructor_WhenEndpointIsMissing_Throws()
    {
        var options = Options.Create(
            new AzureSearchOptions
            {
                Endpoint = string.Empty,
                ApiKey = "test-key"
            });

        var exception = Assert.Throws<InvalidOperationException>(
            () => new SearchIndexManagementService(
                new FakeSearchIndexDefinitionService(),
                options));

        Assert.Equal(
            "Azure Search endpoint is not configured.",
            exception.Message);
    }

    [Fact]
    public void Constructor_WhenApiKeyIsMissing_Throws()
    {
        var options = Options.Create(
            new AzureSearchOptions
            {
                Endpoint = "https://example.search.windows.net",
                ApiKey = string.Empty
            });

        var exception = Assert.Throws<InvalidOperationException>(
            () => new SearchIndexManagementService(
                new FakeSearchIndexDefinitionService(),
                options));

        Assert.Equal(
            "Azure Search API key is not configured.",
            exception.Message);
    }

    [Fact]
    public async Task EnsureIndexAsync_WhenCancellationRequested_Throws()
    {
        var options = Options.Create(
            new AzureSearchOptions
            {
                Endpoint = "https://example.search.windows.net",
                ApiKey = "test-key"
            });

        var service = new SearchIndexManagementService(
            new FakeSearchIndexDefinitionService(),
            options);

        using var cancellationTokenSource =
            new CancellationTokenSource();

        cancellationTokenSource.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(
            () => service.EnsureIndexAsync(
                cancellationTokenSource.Token));
    }

    private sealed class FakeSearchIndexDefinitionService
        : ISearchIndexDefinitionService
    {
        public SearchIndex CreateDefinition()
        {
            return new SearchIndex("code-chunks");
        }
    }
}
