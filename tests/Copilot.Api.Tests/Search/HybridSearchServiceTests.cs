using Copilot.Api.Configuration;
using Copilot.Api.Services.Search;
using Microsoft.Extensions.Options;

namespace Copilot.Api.Tests.Search;

public sealed class HybridSearchServiceTests
{
    [Fact]
    public void Constructor_WhenOptionsAreNull_Throws()
    {
        var exception = Assert.Throws<ArgumentNullException>(
            () => new HybridSearchService(null!));

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
            () => new HybridSearchService(options));

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
            () => new HybridSearchService(options));

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
            () => new HybridSearchService(options));

        Assert.Equal(
            "Azure Search index name is not configured.",
            exception.Message);
    }

    [Fact]
    public async Task SearchAsync_WhenQueryIsEmpty_Throws()
    {
        var service = CreateService();

        await Assert.ThrowsAsync<ArgumentException>(
            () => service.SearchAsync(
                string.Empty,
                new[] { 0.1f, 0.2f },
                5,
                "owner/repository",
                "main"));
    }

    [Fact]
    public async Task SearchAsync_WhenQueryIsWhitespace_Throws()
    {
        var service = CreateService();

        await Assert.ThrowsAsync<ArgumentException>(
            () => service.SearchAsync(
                "   ",
                new[] { 0.1f, 0.2f },
                5,
                "owner/repository",
                "main"));
    }

    [Fact]
    public async Task SearchAsync_WhenEmbeddingIsNull_Throws()
    {
        var service = CreateService();

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => service.SearchAsync(
                "find authentication code",
                null!,
                5,
                "owner/repository",
                "main"));
    }

    [Fact]
    public async Task SearchAsync_WhenEmbeddingIsEmpty_Throws()
    {
        var service = CreateService();

        await Assert.ThrowsAsync<ArgumentException>(
            () => service.SearchAsync(
                "find authentication code",
                Array.Empty<float>(),
                5,
                "owner/repository",
                "main"));
    }

    [Fact]
    public async Task SearchAsync_WhenTopIsZero_Throws()
    {
        var service = CreateService();

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => service.SearchAsync(
                "find authentication code",
                new[] { 0.1f, 0.2f },
                0,
                "owner/repository",
                "main"));
    }

    [Fact]
    public async Task SearchAsync_WhenTopIsNegative_Throws()
    {
        var service = CreateService();

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => service.SearchAsync(
                "find authentication code",
                new[] { 0.1f, 0.2f },
                -1,
                "owner/repository",
                "main"));
    }

    [Fact]
    public async Task SearchAsync_WhenCancellationRequested_Throws()
    {
        var service = CreateService();

        using var cancellationTokenSource =
            new CancellationTokenSource();

        cancellationTokenSource.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(
            () => service.SearchAsync(
                "find authentication code",
                new[] { 0.1f, 0.2f },
                5,
                "owner/repository",
                "main",
                cancellationTokenSource.Token));
    }

    private static HybridSearchService CreateService()
    {
        var options = Options.Create(
            new AzureSearchOptions
            {
                Endpoint = "https://example.search.windows.net",
                ApiKey = "test-key",
                IndexName = "code-chunks"
            });

        return new HybridSearchService(options);
    }
}


