using Copilot.Api.Models.Repository;
using Copilot.Api.Services.Repository;
using Microsoft.Extensions.Configuration;
using Moq;

namespace Copilot.Api.Tests;

public class AzureOpenAIEmbeddingServiceTests
{
    [Fact]
    public async Task GenerateEmbeddingsAsync_EmptyChunks_ReturnsEmptyList()
    {
        var configuration = CreateConfiguration();

        var service = new AzureOpenAIEmbeddingService(
            configuration);

        var result = await service.GenerateEmbeddingsAsync(
            Array.Empty<CodeChunk>());

        Assert.Empty(result);
    }

    [Fact]
    public async Task GenerateEmbeddingsAsync_NullChunks_ThrowsArgumentNullException()
    {
        var configuration = CreateConfiguration();

        var service = new AzureOpenAIEmbeddingService(
            configuration);

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => service.GenerateEmbeddingsAsync(null!));
    }

    [Fact]
    public void Constructor_ThrowsWhenEndpointIsMissing()
    {
        var configuration = CreateConfiguration(
            endpoint: null);

        var exception = Assert.Throws<InvalidOperationException>(
            () => new AzureOpenAIEmbeddingService(configuration));

        Assert.Equal(
            "Azure OpenAI endpoint is not configured.",
            exception.Message);
    }

    [Fact]
    public void Constructor_ThrowsWhenApiKeyIsMissing()
    {
        var configuration = CreateConfiguration(
            apiKey: null);

        var exception = Assert.Throws<InvalidOperationException>(
            () => new AzureOpenAIEmbeddingService(configuration));

        Assert.Equal(
            "Azure OpenAI API key is not configured.",
            exception.Message);
    }

    [Fact]
    public void Constructor_ThrowsWhenEmbeddingDeploymentNameIsMissing()
    {
        var configuration = CreateConfiguration(
            embeddingDeploymentName: null);

        var exception = Assert.Throws<InvalidOperationException>(
            () => new AzureOpenAIEmbeddingService(configuration));

        Assert.Equal(
            "Azure OpenAI embedding deployment name is not configured.",
            exception.Message);
    }

    [Fact]
    public async Task GenerateEmbeddingsAsync_EmptyChunkContent_ThrowsArgumentException()
    {
        var configuration = CreateConfiguration();

        var service = new AzureOpenAIEmbeddingService(
            configuration);

        var chunk = new CodeChunk(
            "owner/repository",
            "main",
            "src/Test.cs",
            "csharp",
            0,
            1,
            1,
            string.Empty);

        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => service.GenerateEmbeddingsAsync(
                new[] { chunk }));

        Assert.Equal(
            "chunks",
            exception.ParamName);

        Assert.Contains(
            "Chunk 0 for 'src/Test.cs' has no content.",
            exception.Message);
    }

    [Fact]
    public async Task GenerateEmbeddingsAsync_CancellationRequested_ThrowsOperationCanceledException()
    {
        var configuration = CreateConfiguration();

        var service = new AzureOpenAIEmbeddingService(
            configuration);

        using var cancellationTokenSource =
            new CancellationTokenSource();

        cancellationTokenSource.Cancel();

        var chunk = new CodeChunk(
            "owner/repository",
            "main",
            "src/Test.cs",
            "csharp",
            0,
            1,
            1,
            "Console.WriteLine(\"Hello\");");

        await Assert.ThrowsAsync<OperationCanceledException>(
            () => service.GenerateEmbeddingsAsync(
                new[] { chunk },
                cancellationTokenSource.Token));
    }

    private static IConfiguration CreateConfiguration(
        string? endpoint = "https://example.openai.azure.com",
        string? apiKey = "test-api-key",
        string? embeddingDeploymentName = "text-embedding-3-small")
    {
        var configuration = new Mock<IConfiguration>();

        configuration
            .Setup(x => x["AzureOpenAI:Endpoint"])
            .Returns(endpoint);

        configuration
            .Setup(x => x["AzureOpenAI:ApiKey"])
            .Returns(apiKey);

        configuration
            .Setup(x => x["AzureOpenAI:EmbeddingDeploymentName"])
            .Returns(embeddingDeploymentName);

        return configuration.Object;
    }
}
