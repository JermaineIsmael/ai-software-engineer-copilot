using Copilot.Api.Models.Copilot;

namespace Copilot.Api.Tests.Copilot;

public sealed class CopilotResponseTests
{
    [Fact]
    public void Constructor_PreservesValues()
    {
        var sources = new[]
        {
            new CopilotSource(
                "repo",
                "main",
                "src/Handler.cs",
                10,
                20,
                0.95)
        };

        var response = new CopilotResponse(
            "Where is the handler?",
            "The handler is in Handler.cs.",
            sources);

        Assert.Equal(
            "Where is the handler?",
            response.Query);

        Assert.Equal(
            "The handler is in Handler.cs.",
            response.Answer);

        Assert.Single(response.Sources);
    }
}
