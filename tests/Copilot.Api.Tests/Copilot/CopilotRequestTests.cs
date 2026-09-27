using Copilot.Api.Models.Copilot;

namespace Copilot.Api.Tests.Copilot;

public sealed class CopilotRequestTests
{
    [Fact]
    public void Constructor_PreservesValues()
    {
        var request = new CopilotRequest(
            "Where is the Service Bus handler?",
            10);

        Assert.Equal(
            "Where is the Service Bus handler?",
            request.Query);

        Assert.Equal(10, request.Top);
    }
}
