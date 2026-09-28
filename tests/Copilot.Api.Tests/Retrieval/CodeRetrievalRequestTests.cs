using Copilot.Api.Models.Retrieval;

namespace Copilot.Api.Tests.Retrieval;

public sealed class CodeRetrievalRequestTests
{
    [Fact]
    public void Constructor_ShouldStoreValues()
    {
        var request = new CodeRetrievalRequest(
            "authentication",
            "owner/repository",
            "main",
            10);

        Assert.Equal(
            "authentication",
            request.Query);

        Assert.Equal(
            "owner/repository",
            request.Repository);

        Assert.Equal(
            "main",
            request.Branch);

        Assert.Equal(
            10,
            request.Top);
    }

    [Fact]
    public void Constructor_ShouldDefaultBranchAndTop()
    {
        var request = new CodeRetrievalRequest(
            "authentication",
            "owner/repository");

        Assert.Equal(
            "authentication",
            request.Query);

        Assert.Equal(
            "owner/repository",
            request.Repository);

        Assert.Equal(
            "main",
            request.Branch);

        Assert.Equal(
            5,
            request.Top);
    }
}
