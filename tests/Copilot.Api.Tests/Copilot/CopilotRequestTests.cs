using Copilot.Api.Models.Copilot;

namespace Copilot.Api.Tests.Copilot;

public sealed class CopilotRequestTests
{
    [Fact]
    public void Constructor_ShouldStoreValues()
    {
        var request = new CopilotRequest(
            "How is authentication handled?",
            "owner/repository",
            "main",
            10);

        Assert.Equal(
            "How is authentication handled?",
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
}
