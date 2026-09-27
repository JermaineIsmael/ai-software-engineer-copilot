using Copilot.Api.Models.Repository;
using Copilot.Api.Services.Repository;
using Copilot.Api.Services.Retrieval;

namespace Copilot.Api.Tests.Retrieval;

public sealed class QueryEmbeddingServiceTests
{
    [Fact]
    public async Task GenerateAsync_ShouldReturnQueryEmbedding()
    {
        var expectedEmbedding = new float[]
        {
            0.1f,
            0.2f,
            0.3f
        };

        var embeddingService = new FakeEmbeddingService(
            new[]
            {
                CreateEmbedding(
                    "How is authentication handled?",
                    expectedEmbedding)
            });

        var service = new QueryEmbeddingService(
            embeddingService);

        var result = await service.GenerateAsync(
            "How is authentication handled?");

        Assert.Equal(expectedEmbedding, result);

        Assert.Equal(
            "How is authentication handled?",
            embeddingService.ReceivedChunks.Single().Content);
    }

    [Fact]
    public async Task GenerateAsync_ShouldCreateTextQueryChunk()
    {
        var embeddingService = new FakeEmbeddingService(
            new[]
            {
                CreateEmbedding(
                    "find authentication code",
                    new float[] { 0.1f })
            });

        var service = new QueryEmbeddingService(
            embeddingService);

        await service.GenerateAsync(
            "find authentication code");

        var chunk = embeddingService.ReceivedChunks.Single();

        Assert.Equal(string.Empty, chunk.Repository);
        Assert.Equal(string.Empty, chunk.Branch);
        Assert.Equal(string.Empty, chunk.Path);
        Assert.Equal("text", chunk.Language);
        Assert.Equal(0, chunk.ChunkIndex);
        Assert.Equal(0, chunk.StartLine);
        Assert.Equal(0, chunk.EndLine);
        Assert.Equal(
            "find authentication code",
            chunk.Content);
    }

    [Fact]
    public async Task GenerateAsync_ShouldRejectEmptyQuery()
    {
        var service = CreateService();

        await Assert.ThrowsAsync<ArgumentException>(
            () => service.GenerateAsync(string.Empty));
    }

    [Fact]
    public async Task GenerateAsync_ShouldRejectWhitespaceQuery()
    {
        var service = CreateService();

        await Assert.ThrowsAsync<ArgumentException>(
            () => service.GenerateAsync("   "));
    }

    [Fact]
    public async Task GenerateAsync_ShouldThrowWhenEmbeddingServiceReturnsNoResults()
    {
        var embeddingService = new FakeEmbeddingService(
            Array.Empty<CodeChunkEmbedding>());

        var service = new QueryEmbeddingService(
            embeddingService);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.GenerateAsync("authentication"));
    }

    [Fact]
    public async Task GenerateAsync_ShouldThrowWhenEmbeddingServiceReturnsMultipleResults()
    {
        var embeddingService = new FakeEmbeddingService(
            new[]
            {
                CreateEmbedding(
                    "first",
                    new float[] { 0.1f }),

                CreateEmbedding(
                    "second",
                    new float[] { 0.2f })
            });

        var service = new QueryEmbeddingService(
            embeddingService);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.GenerateAsync("authentication"));
    }

    [Fact]
    public async Task GenerateAsync_ShouldRespectCancellation()
    {
        var service = CreateService();

        using var cancellationTokenSource =
            new CancellationTokenSource();

        cancellationTokenSource.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => service.GenerateAsync(
                "authentication",
                cancellationTokenSource.Token));
    }

    [Fact]
    public void Constructor_ShouldRejectNullEmbeddingService()
    {
        Assert.Throws<ArgumentNullException>(
            () => new QueryEmbeddingService(null!));
    }

    private static QueryEmbeddingService CreateService()
    {
        return new QueryEmbeddingService(
            new FakeEmbeddingService(
                new[]
                {
                    CreateEmbedding(
                        "authentication",
                        new float[] { 0.1f })
                }));
    }

    private static CodeChunkEmbedding CreateEmbedding(
        string content,
        IReadOnlyList<float> embedding)
    {
        return new CodeChunkEmbedding(
            string.Empty,
            string.Empty,
            string.Empty,
            "text",
            0,
            0,
            0,
            content,
            embedding);
    }

    private sealed class FakeEmbeddingService : IEmbeddingService
    {
        private readonly IReadOnlyList<CodeChunkEmbedding> _results;

        public FakeEmbeddingService(
            IReadOnlyList<CodeChunkEmbedding> results)
        {
            _results = results;
        }

        public List<CodeChunk> ReceivedChunks { get; } = new();

        public Task<IReadOnlyList<CodeChunkEmbedding>>
            GenerateEmbeddingsAsync(
                IReadOnlyList<CodeChunk> chunks,
                CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            ReceivedChunks.AddRange(chunks);

            return Task.FromResult(_results);
        }
    }
}
