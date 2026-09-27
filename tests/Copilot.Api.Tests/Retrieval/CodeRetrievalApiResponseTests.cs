using Copilot.Api.Models.Retrieval;

namespace Copilot.Api.Tests.Retrieval;

public sealed class CodeRetrievalApiResponseTests
{
    [Fact]
    public void Constructor_ShouldPreserveQueryResultsAndContext()
    {
        var chunk = new RetrievedCodeChunk(
            "chunk-1",
            "test-repository",
            "main",
            "src/Test.cs",
            "csharp",
            0,
            1,
            10,
            "public class Test {}",
            0.95);

        var context = new CodeRetrievalContext(
            "test context",
            new[] { chunk });

        var response = new CodeRetrievalApiResponse(
            "find Test class",
            new[] { chunk },
            context);

        Assert.Equal(
            "find Test class",
            response.Query);

        Assert.Single(response.Results);
        Assert.Equal(
            "chunk-1",
            response.Results[0].Id);

        Assert.Equal(
            "test context",
            response.Context.Text);

        Assert.Single(response.Context.Chunks);
    }
}
