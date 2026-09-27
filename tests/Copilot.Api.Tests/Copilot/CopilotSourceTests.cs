using Copilot.Api.Models.Copilot;

namespace Copilot.Api.Tests.Copilot;

public sealed class CopilotSourceTests
{
    [Fact]
    public void Constructor_PreservesValues()
    {
        var source = new CopilotSource(
            "repo",
            "main",
            "src/Handler.cs",
            10,
            20,
            0.95);

        Assert.Equal("repo", source.Repository);
        Assert.Equal("main", source.Branch);
        Assert.Equal("src/Handler.cs", source.Path);
        Assert.Equal(10, source.StartLine);
        Assert.Equal(20, source.EndLine);
        Assert.Equal(0.95, source.Score);
    }
}
