using Copilot.Api.Configuration;
using Copilot.Api.Models.Search;
using Copilot.Api.Services.Retrieval;
using Microsoft.Extensions.Options;

namespace Copilot.Api.Tests.Retrieval;

public sealed class RetrievalResultProcessorTests
{
    [Fact]
    public void Process_ShouldFilterResultsBelowMinimumScore()
    {
        var processor = CreateProcessor(minimumScore: 0.5);

        var results = new[]
        {
            CreateResult("high", 0.9),
            CreateResult("low", 0.4),
            CreateResult("medium", 0.6)
        };

        var processed = processor.Process(results, 10);

        Assert.Equal(2, processed.Count);
        Assert.DoesNotContain(
            processed,
            result => result.Id == "low");
    }

    [Fact]
    public void Process_ShouldOrderResultsByDescendingScore()
    {
        var processor = CreateProcessor();

        var results = new[]
        {
            CreateResult("low", 0.2),
            CreateResult("high", 0.9),
            CreateResult("medium", 0.5)
        };

        var processed = processor.Process(results, 10);

        Assert.Equal(
            new[] { "high", "medium", "low" },
            processed.Select(result => result.Id));
    }

    [Fact]
    public void Process_ShouldLimitResultsToTop()
    {
        var processor = CreateProcessor();

        var results = new[]
        {
            CreateResult("one", 0.9),
            CreateResult("two", 0.8),
            CreateResult("three", 0.7)
        };

        var processed = processor.Process(results, 2);

        Assert.Equal(2, processed.Count);
        Assert.Equal(
            new[] { "one", "two" },
            processed.Select(result => result.Id));
    }

    [Fact]
    public void Process_ShouldRespectMaximumTop()
    {
        var processor = CreateProcessor(maxTop: 2);

        var results = new[]
        {
            CreateResult("one", 0.9),
            CreateResult("two", 0.8),
            CreateResult("three", 0.7)
        };

        var processed = processor.Process(results, 10);

        Assert.Equal(2, processed.Count);
    }

    [Fact]
    public void Process_ShouldUsePathAsDeterministicTieBreaker()
    {
        var processor = CreateProcessor();

        var results = new[]
        {
            CreateResult("b", 0.8, "z.cs"),
            CreateResult("a", 0.8, "a.cs"),
            CreateResult("c", 0.8, "m.cs")
        };

        var processed = processor.Process(results, 10);

        Assert.Equal(
            new[] { "a", "c", "b" },
            processed.Select(result => result.Id));
    }

    [Fact]
    public void Process_ShouldUseChunkIndexAsSecondTieBreaker()
    {
        var processor = CreateProcessor();

        var results = new[]
        {
            CreateResult("chunk2", 0.8, "file.cs", 2),
            CreateResult("chunk0", 0.8, "file.cs", 0),
            CreateResult("chunk1", 0.8, "file.cs", 1)
        };

        var processed = processor.Process(results, 10);

        Assert.Equal(
            new[] { "chunk0", "chunk1", "chunk2" },
            processed.Select(result => result.Id));
    }

    [Fact]
    public void Process_ShouldRejectInvalidTop()
    {
        var processor = CreateProcessor();

        Assert.Throws<ArgumentOutOfRangeException>(
            () => processor.Process(
                Array.Empty<HybridSearchResult>(),
                0));
    }

    [Fact]
    public void Process_ShouldRejectNullResults()
    {
        var processor = CreateProcessor();

        Assert.Throws<ArgumentNullException>(
            () => processor.Process(null!, 5));
    }

    private static RetrievalResultProcessor CreateProcessor(
        double minimumScore = 0.0,
        int maxTop = 20)
    {
        var options = Options.Create(
            new RetrievalOptions
            {
                MinimumScore = minimumScore,
                MaxTop = maxTop
            });

        return new RetrievalResultProcessor(options);
    }

    private static HybridSearchResult CreateResult(
        string id,
        double score,
        string path = "file.cs",
        int chunkIndex = 0)
    {
        return new HybridSearchResult(
            id,
            "test-repository",
            "main",
            path,
            "csharp",
            chunkIndex,
            1,
            10,
            $"content-{id}",
            score);
    }
}
