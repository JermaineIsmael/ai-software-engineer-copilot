using Copilot.Api.Models.Retrieval;

namespace Copilot.Api.Tests.Retrieval;

public sealed class CodeRetrievalResponseTests
{
    [Fact]
    public void Constructor_ShouldStoreQueryAndResults()
    {
        var result = new RetrievedCodeChunk(
            "id-1",
            "owner/repository",
            "main",
            "src/AuthService.cs",
            "csharp",
            0,
            1,
            20,
            "public class AuthService {}",
            0.95);

        var results = new List<RetrievedCodeChunk>
        {
            result
        };

        var response = new CodeRetrievalResponse(
            "How is authentication handled?",
            results);

        Assert.Equal(
            "How is authentication handled?",
            response.Query);

        Assert.Single(response.Results);
        Assert.Equal("id-1", response.Results[0].Id);
        Assert.Equal("src/AuthService.cs", response.Results[0].Path);
        Assert.Equal(0.95, response.Results[0].Score);
    }

    [Fact]
    public void RetrievedCodeChunk_ShouldStoreAllProperties()
    {
        var result = new RetrievedCodeChunk(
            "id-1",
            "owner/repository",
            "main",
            "src/AuthService.cs",
            "csharp",
            2,
            41,
            80,
            "authentication code",
            0.88);

        Assert.Equal("id-1", result.Id);
        Assert.Equal("owner/repository", result.Repository);
        Assert.Equal("main", result.Branch);
        Assert.Equal("src/AuthService.cs", result.Path);
        Assert.Equal("csharp", result.Language);
        Assert.Equal(2, result.ChunkIndex);
        Assert.Equal(41, result.StartLine);
        Assert.Equal(80, result.EndLine);
        Assert.Equal("authentication code", result.Content);
        Assert.Equal(0.88, result.Score);
    }
}
