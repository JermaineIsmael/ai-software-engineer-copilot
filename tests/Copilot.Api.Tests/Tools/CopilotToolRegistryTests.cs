using Copilot.Api.Services.Tools;

namespace Copilot.Api.Tests.Tools;

public sealed class CopilotToolRegistryTests
{
    [Fact]
    public void GetTools_ReturnsRegisteredTools()
    {
        var first = new TestTool("first");
        var second = new TestTool("second");

        var registry =
            new CopilotToolRegistry(
                [
                    first,
                    second
                ]);

        var tools = registry.GetTools();

        Assert.Equal(2, tools.Count);
        Assert.Contains(first, tools);
        Assert.Contains(second, tools);
    }

    [Fact]
    public void GetTool_IsCaseInsensitive()
    {
        var tool = new TestTool("code_search");

        var registry =
            new CopilotToolRegistry([tool]);

        var result =
            registry.GetTool("CODE_SEARCH");

        Assert.Same(tool, result);
    }

    [Fact]
    public void GetTool_ReturnsNullForUnknownTool()
    {
        var registry =
            new CopilotToolRegistry(
                [new TestTool("code_search")]);

        var result =
            registry.GetTool("unknown");

        Assert.Null(result);
    }

    private sealed class TestTool : ICopilotTool
    {
        public TestTool(string name)
        {
            Name = name;
        }

        public string Name { get; }

        public string Description =>
            "Test tool.";

        public string ParametersJsonSchema =>
            """{"type":"object"}""";

        public Task<string> ExecuteAsync(
            string argumentsJson,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult("{}");
        }
    }
}
