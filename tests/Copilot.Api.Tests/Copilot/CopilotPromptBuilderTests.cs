using Copilot.Api.Services.Copilot;

namespace Copilot.Api.Tests.Copilot;

public sealed class CopilotPromptBuilderTests
{
    [Fact]
    public void BuildSystemPrompt_ContainsGroundingRules()
    {
        var prompt =
            CopilotPromptBuilder.BuildSystemPrompt();

        Assert.Contains(
            "repository context",
            prompt,
            StringComparison.OrdinalIgnoreCase);

        Assert.Contains(
            "Do not invent",
            prompt,
            StringComparison.OrdinalIgnoreCase);

        Assert.Contains(
            "insufficient",
            prompt,
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void BuildSystemPrompt_ContainsSourceCitationRules()
    {
        var prompt =
            CopilotPromptBuilder.BuildSystemPrompt();

        Assert.Contains(
            "[Source:",
            prompt,
            StringComparison.Ordinal);

        Assert.Contains(
            "line range",
            prompt,
            StringComparison.OrdinalIgnoreCase);
    }
}
