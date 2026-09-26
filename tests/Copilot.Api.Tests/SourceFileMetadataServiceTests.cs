using Copilot.Api.Models.Repository;
using Copilot.Api.Services.Repository;

namespace Copilot.Api.Tests;

public class SourceFileMetadataServiceTests
{
    private readonly SourceFileMetadataService _service;

    public SourceFileMetadataServiceTests()
    {
        _service = new SourceFileMetadataService(
            new SourceFileClassifier());
    }

    [Fact]
    public void Extract_ReturnsExpectedMetadata()
    {
        var file = new RepositoryFile(
            "owner/repository",
            "main",
            "src/Program.cs",
            "cs",
            "line one\nline two\nline three");

        var result = _service.Extract(file);

        Assert.Equal("owner/repository", result.Repository);
        Assert.Equal("main", result.Branch);
        Assert.Equal("src/Program.cs", result.Path);
        Assert.Equal("csharp", result.Language);
        Assert.Equal(".cs", result.Extension);
        Assert.Equal(3, result.LineCount);
        Assert.Equal(file.Content.Length, result.CharacterCount);
        Assert.Equal(
            System.Text.Encoding.UTF8.GetByteCount(file.Content),
            result.ByteCount);
        Assert.Equal(file.Content, result.Content);
    }

    [Fact]
    public void Extract_CountsLinesWithoutTrailingNewline()
    {
        var file = new RepositoryFile(
            "owner/repository",
            "main",
            "README",
            "text",
            "line one\nline two");

        var result = _service.Extract(file);

        Assert.Equal(2, result.LineCount);
    }

    [Fact]
    public void Extract_CountsLinesWithTrailingNewline()
    {
        var file = new RepositoryFile(
            "owner/repository",
            "main",
            "README",
            "text",
            "line one\nline two\n");

        var result = _service.Extract(file);

        Assert.Equal(2, result.LineCount);
    }

    [Fact]
    public void Extract_HandlesEmptyContent()
    {
        var file = new RepositoryFile(
            "owner/repository",
            "main",
            "empty.txt",
            "text",
            string.Empty);

        var result = _service.Extract(file);

        Assert.Equal(0, result.LineCount);
        Assert.Equal(0, result.CharacterCount);
        Assert.Equal(0, result.ByteCount);
        Assert.Equal(string.Empty, result.Content);
    }

    [Fact]
    public void Extract_CalculatesUtf8ByteCountForUnicodeContent()
    {
        var content = "Hello ?? ??";

        var file = new RepositoryFile(
            "owner/repository",
            "main",
            "README",
            "text",
            content);

        var result = _service.Extract(file);

        Assert.Equal(content.Length, result.CharacterCount);
        Assert.Equal(
            System.Text.Encoding.UTF8.GetByteCount(content),
            result.ByteCount);
    }

    [Fact]
    public void Extract_ThrowsWhenFileIsNull()
    {
        Assert.Throws<ArgumentNullException>(
            () => _service.Extract(null!));
    }
}
