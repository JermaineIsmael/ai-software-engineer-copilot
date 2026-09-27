using Copilot.Api.Models.Retrieval;
using Copilot.Api.Models.Search;
using Copilot.Api.Services.Retrieval;
using Copilot.Api.Services.Search;

namespace Copilot.Api.Tests.Retrieval;

public sealed class CodeRetrievalServiceTests
{
    [Fact]
    public async Task RetrieveAsync_ShouldGenerateEmbeddingAndSearch()
    {
        var expectedEmbedding = new float[]
        {
            0.1f,
            0.2f,
            0.3f
        };

        var expectedSearchResult = CreateSearchResult();

        var embeddingService = new FakeQueryEmbeddingService(
            expectedEmbedding);

        var searchService = new FakeHybridSearchService(
            new[]
            {
                expectedSearchResult
            });

        var service = new CodeRetrievalService(
            embeddingService,
            searchService,
            new FakeRetrievalResultProcessor());

        var request = new CodeRetrievalRequest(
            "How is authentication handled?",
            5);

        var response = await service.RetrieveAsync(request);

        Assert.Equal(
            request.Query,
            embeddingService.ReceivedQuery);

        Assert.Equal(
            request.Query,
            searchService.ReceivedQuery);

        Assert.Equal(
            expectedEmbedding,
            searchService.ReceivedEmbedding);

        Assert.Equal(
            request.Top,
            searchService.ReceivedTop);

        Assert.Equal(
            request.Query,
            response.Query);

        Assert.Single(response.Results);
    }

    [Fact]
    public async Task RetrieveAsync_ShouldMapSearchResults()
    {
        var searchResult = CreateSearchResult();

        var service = CreateService(
            new[]
            {
                searchResult
            });

        var response = await service.RetrieveAsync(
            new CodeRetrievalRequest(
                "authentication",
                5));

        var result = response.Results.Single();

        Assert.Equal(searchResult.Id, result.Id);
        Assert.Equal(
            searchResult.Repository,
            result.Repository);
        Assert.Equal(
            searchResult.Branch,
            result.Branch);
        Assert.Equal(
            searchResult.Path,
            result.Path);
        Assert.Equal(
            searchResult.Language,
            result.Language);
        Assert.Equal(
            searchResult.ChunkIndex,
            result.ChunkIndex);
        Assert.Equal(
            searchResult.StartLine,
            result.StartLine);
        Assert.Equal(
            searchResult.EndLine,
            result.EndLine);
        Assert.Equal(
            searchResult.Content,
            result.Content);
        Assert.Equal(
            searchResult.Score,
            result.Score);
    }

    [Fact]
    public async Task RetrieveAsync_ShouldReturnEmptyResults()
    {
        var service = CreateService(
            Array.Empty<HybridSearchResult>());

        var response = await service.RetrieveAsync(
            new CodeRetrievalRequest(
                "authentication",
                5));

        Assert.Equal(
            "authentication",
            response.Query);

        Assert.Empty(response.Results);
    }

    [Fact]
    public async Task RetrieveAsync_ShouldRejectNullRequest()
    {
        var service = CreateService(
            Array.Empty<HybridSearchResult>());

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => service.RetrieveAsync(null!));
    }

    [Fact]
    public async Task RetrieveAsync_ShouldRejectEmptyQuery()
    {
        var service = CreateService(
            Array.Empty<HybridSearchResult>());

        await Assert.ThrowsAsync<ArgumentException>(
            () => service.RetrieveAsync(
                new CodeRetrievalRequest(
                    string.Empty,
                    5)));
    }

    [Fact]
    public async Task RetrieveAsync_ShouldRejectWhitespaceQuery()
    {
        var service = CreateService(
            Array.Empty<HybridSearchResult>());

        await Assert.ThrowsAsync<ArgumentException>(
            () => service.RetrieveAsync(
                new CodeRetrievalRequest(
                    "   ",
                    5)));
    }

    [Fact]
    public async Task RetrieveAsync_ShouldRejectInvalidTop()
    {
        var service = CreateService(
            Array.Empty<HybridSearchResult>());

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => service.RetrieveAsync(
                new CodeRetrievalRequest(
                    "authentication",
                    0)));
    }

    [Fact]
    public async Task RetrieveAsync_ShouldRespectCancellation()
    {
        var service = CreateService(
            Array.Empty<HybridSearchResult>());

        using var cancellationTokenSource =
            new CancellationTokenSource();

        cancellationTokenSource.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => service.RetrieveAsync(
                new CodeRetrievalRequest(
                    "authentication",
                    5),
                cancellationTokenSource.Token));
    }

    [Fact]
    public async Task RetrieveAsync_ShouldPropagateEmbeddingFailure()
    {
        var embeddingService =
            new FailingQueryEmbeddingService();

        var searchService =
            new FakeHybridSearchService(
                Array.Empty<HybridSearchResult>());

        var service = new CodeRetrievalService(
            embeddingService,
            searchService,
            new FakeRetrievalResultProcessor());

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.RetrieveAsync(
                new CodeRetrievalRequest(
                    "authentication",
                    5)));

        Assert.Null(searchService.ReceivedQuery);
    }

    [Fact]
    public async Task RetrieveAsync_ShouldPropagateSearchFailure()
    {
        var embeddingService =
            new FakeQueryEmbeddingService(
                new float[] { 0.1f });

        var searchService =
            new FailingHybridSearchService();

        var service = new CodeRetrievalService(
            embeddingService,
            searchService,
            new FakeRetrievalResultProcessor());

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.RetrieveAsync(
                new CodeRetrievalRequest(
                    "authentication",
                    5)));
    }

    [Fact]
    public void Constructor_ShouldRejectNullEmbeddingService()
    {
        Assert.Throws<ArgumentNullException>(
            () => new CodeRetrievalService(
                null!,
                new FakeHybridSearchService(
                    Array.Empty<HybridSearchResult>()),
                new FakeRetrievalResultProcessor()));
    }

    [Fact]
    public void Constructor_ShouldRejectNullSearchService()
    {
        Assert.Throws<ArgumentNullException>(
            () => new CodeRetrievalService(
                new FakeQueryEmbeddingService(
                    new float[] { 0.1f }),
                null!,
                new FakeRetrievalResultProcessor()));
    }

    private static CodeRetrievalService CreateService(
        IReadOnlyList<HybridSearchResult> results)
    {
        return new CodeRetrievalService(
            new FakeQueryEmbeddingService(
                new float[] { 0.1f }),
            new FakeHybridSearchService(results),
            new FakeRetrievalResultProcessor());
    }

    private static HybridSearchResult CreateSearchResult()
    {
        return new HybridSearchResult(
            "result-1",
            "owner/repository",
            "main",
            "src/AuthService.cs",
            "csharp",
            2,
            41,
            80,
            "public class AuthService {}",
            0.92);
    }

    private sealed class FakeQueryEmbeddingService
        : IQueryEmbeddingService
    {
        private readonly IReadOnlyList<float> _embedding;

        public FakeQueryEmbeddingService(
            IReadOnlyList<float> embedding)
        {
            _embedding = embedding;
        }

        public string? ReceivedQuery { get; private set; }

        public Task<IReadOnlyList<float>> GenerateAsync(
            string query,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            ReceivedQuery = query;

            return Task.FromResult(_embedding);
        }
    }

    private sealed class FailingQueryEmbeddingService
        : IQueryEmbeddingService
    {
        public Task<IReadOnlyList<float>> GenerateAsync(
            string query,
            CancellationToken cancellationToken = default)
        {
            throw new InvalidOperationException(
                "Simulated query embedding failure.");
        }
    }

    private sealed class FakeHybridSearchService
        : IHybridSearchService
    {
        private readonly IReadOnlyList<HybridSearchResult> _results;

        public FakeHybridSearchService(
            IReadOnlyList<HybridSearchResult> results)
        {
            _results = results;
        }

        public string? ReceivedQuery { get; private set; }

        public IReadOnlyList<float>? ReceivedEmbedding
        {
            get;
            private set;
        }

        public int ReceivedTop { get; private set; }

        public Task<IReadOnlyList<HybridSearchResult>> SearchAsync(
            string query,
            IReadOnlyList<float> queryEmbedding,
            int top = 5,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            ReceivedQuery = query;
            ReceivedEmbedding = queryEmbedding;
            ReceivedTop = top;

            return Task.FromResult(_results);
        }
    }

    private sealed class FailingHybridSearchService
        : IHybridSearchService
    {
        public Task<IReadOnlyList<HybridSearchResult>> SearchAsync(
            string query,
            IReadOnlyList<float> queryEmbedding,
            int top = 5,
            CancellationToken cancellationToken = default)
        {
            throw new InvalidOperationException(
                "Simulated hybrid search failure.");
        }
    }
}





