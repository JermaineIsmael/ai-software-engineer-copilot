using Copilot.Api.Configuration;
using Copilot.Api.Models.Repository;
using Copilot.Api.Services.Search;
using Microsoft.Extensions.Options;

namespace Copilot.Api.Tests.Search;

public sealed class SearchDocumentIndexingServiceTests
{
    [Fact]
    public void Constructor_WhenOptionsAreNull_Throws()
    {
        var exception = Assert.Throws<ArgumentNullException>(
            () => new SearchDocumentIndexingService(null!));

        Assert.Equal("options", exception.ParamName);
    }

    [Fact]
    public void Constructor_WhenEndpointIsMissing_Throws()
    {
        var options = Options.Create(
            new AzureSearchOptions
            {
                Endpoint = string.Empty,
                ApiKey = "test-key",
                IndexName = "code-chunks"
            });

        var exception = Assert.Throws<InvalidOperationException>(
            () => new SearchDocumentIndexingService(options));

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
                ApiKey = string.Empty,
                IndexName = "code-chunks"
            });

        var exception = Assert.Throws<InvalidOperationException>(
            () => new SearchDocumentIndexingService(options));

        Assert.Equal(
            "Azure Search API key is not configured.",
            exception.Message);
    }

    [Fact]
    public void Constructor_WhenIndexNameIsMissing_Throws()
    {
        var options = Options.Create(
            new AzureSearchOptions
            {
                Endpoint = "https://example.search.windows.net",
                ApiKey = "test-key",
                IndexName = string.Empty
            });

        var exception = Assert.Throws<InvalidOperationException>(
            () => new SearchDocumentIndexingService(options));

        Assert.Equal(
            "Azure Search index name is not configured.",
            exception.Message);
    }

    [Fact]
    public async Task IndexAsync_WhenEmbeddingsAreNull_Throws()
    {
        var service = CreateService();

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => service.IndexAsync(null!));
    }

    [Fact]
    public async Task IndexAsync_WhenEmbeddingsAreEmpty_ReturnsWithoutCallingAzure()
    {
        var service = CreateService();

        await service.IndexAsync(Array.Empty<CodeChunkEmbedding>());
    }

    [Fact]
    public async Task IndexAsync_WhenCancellationRequested_Throws()
    {
        var service = CreateService();

        using var cancellationTokenSource =
            new CancellationTokenSource();

        cancellationTokenSource.Cancel();

        var embedding = new CodeChunkEmbedding(
            "owner/repository",
            "main",
            "src/Example.cs",
            "csharp",
            0,
            1,
            5,
            "public class Example { }",
            new float[] { 0.1f, 0.2f });

        await Assert.ThrowsAsync<OperationCanceledException>(
            () => service.IndexAsync(
                new[] { embedding },
                cancellationTokenSource.Token));
    }

    private static SearchDocumentIndexingService CreateService()
    {
        var options = Options.Create(
            new AzureSearchOptions
            {
                Endpoint = "https://example.search.windows.net",
                ApiKey = "test-key",
                IndexName = "code-chunks"
            });

        return new SearchDocumentIndexingService(options);
    }
}
