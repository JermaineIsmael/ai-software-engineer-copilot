using Copilot.Api.Models.Retrieval;

namespace Copilot.Api.Tests.Retrieval;

public sealed class CodeRetrievalRequestTests
{
    [Fact]
    public void Constructor_ShouldStoreQueryAndTop()
    {
        var request = new CodeRetrievalRequest(
            "How is authentication handled?",
            10);

        Assert.Equal(
            "How is authentication handled?",
            request.Query);

        Assert.Equal(10, request.Top);
    }

    [Fact]
    public void Constructor_ShouldUseDefaultTop()
    {
        var request = new CodeRetrievalRequest(
            "How is authentication handled?");

        Assert.Equal(5, request.Top);
    }
}
