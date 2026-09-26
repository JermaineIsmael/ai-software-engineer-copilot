using Copilot.Api.Services.Repository;

namespace Copilot.Api.Tests;

public class SourceFileClassifierTests
{
    private readonly SourceFileClassifier _classifier = new();

    [Theory]
    [InlineData("Program.cs", "csharp", ".cs")]
    [InlineData("appsettings.json", "json", ".json")]
    [InlineData("deploy.bicep", "bicep", ".bicep")]
    [InlineData("main.tf", "terraform", ".tf")]
    [InlineData("script.ts", "typescript", ".ts")]
    [InlineData("component.tsx", "typescript", ".tsx")]
    [InlineData("styles.scss", "scss", ".scss")]
    [InlineData("query.sql", "sql", ".sql")]
    [InlineData("README.md", "markdown", ".md")]
    public void Classify_ReturnsExpectedLanguageAndExtension(
        string path,
        string expectedLanguage,
        string expectedExtension)
    {
        var language = _classifier.GetLanguage(path);
        var extension = _classifier.GetExtension(path);

        Assert.Equal(expectedLanguage, language);
        Assert.Equal(expectedExtension, extension);
    }

    [Fact]
    public void Dockerfile_ReturnsDockerfileLanguageAndNoExtension()
    {
        var language = _classifier.GetLanguage("Dockerfile");
        var extension = _classifier.GetExtension("Dockerfile");

        Assert.Equal("dockerfile", language);
        Assert.Equal(string.Empty, extension);
    }

    [Theory]
    [InlineData("README", "markdown")]
    [InlineData("README.txt", "text")]
    [InlineData("LICENSE", "text")]
    [InlineData("some-config", "text")]
    public void ExtensionlessOrSpecialFiles_ReturnExpectedLanguage(
        string path,
        string expectedLanguage)
    {
        var language = _classifier.GetLanguage(path);

        Assert.Equal(expectedLanguage, language);
    }

    [Fact]
    public void GetLanguage_ThrowsWhenPathIsMissing()
    {
        Assert.Throws<ArgumentException>(
            () => _classifier.GetLanguage(""));
    }

    [Fact]
    public void GetExtension_ThrowsWhenPathIsMissing()
    {
        Assert.Throws<ArgumentException>(
            () => _classifier.GetExtension(""));
    }
}
