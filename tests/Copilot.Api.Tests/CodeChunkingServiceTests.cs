using Copilot.Api.Models.Repository;
using Copilot.Api.Services.Repository;

namespace Copilot.Api.Tests;

public class CodeChunkingServiceTests
{
    [Fact]
    public void Chunk_EmptyFile_ReturnsNoChunks()
    {
        var file = CreateFile(string.Empty);

        var service = new CodeChunkingService();

        var result = service.Chunk(file);

        Assert.Empty(result);
    }

    [Fact]
    public void Chunk_SmallFile_ReturnsSingleChunk()
    {
        var content = "line 1\nline 2\nline 3";

        var file = CreateFile(content);

        var service = new CodeChunkingService();

        var result = service.Chunk(file);

        var chunk = Assert.Single(result);

        Assert.Equal(0, chunk.ChunkIndex);
        Assert.Equal(1, chunk.StartLine);
        Assert.Equal(3, chunk.EndLine);
        Assert.Equal(content, chunk.Content);
    }

    [Fact]
    public void Chunk_ExactlyMaxLines_ReturnsSingleChunk()
    {
        var content = CreateNumberedLines(40);

        var file = CreateFile(content);

        var service = new CodeChunkingService();

        var result = service.Chunk(file);

        var chunk = Assert.Single(result);

        Assert.Equal(0, chunk.ChunkIndex);
        Assert.Equal(1, chunk.StartLine);
        Assert.Equal(40, chunk.EndLine);
    }

    [Fact]
    public void Chunk_LargerFile_ReturnsMultipleChunks()
    {
        var content = CreateNumberedLines(75);

        var file = CreateFile(content);

        var service = new CodeChunkingService();

        var result = service.Chunk(file);

        Assert.Equal(2, result.Count);

        Assert.Equal(0, result[0].ChunkIndex);
        Assert.Equal(1, result[0].StartLine);
        Assert.Equal(40, result[0].EndLine);

        Assert.Equal(1, result[1].ChunkIndex);
        Assert.Equal(36, result[1].StartLine);
        Assert.Equal(75, result[1].EndLine);
    }

    [Fact]
    public void Chunk_PreservesOverlapBetweenChunks()
    {
        var content = CreateNumberedLines(75);

        var file = CreateFile(content);

        var service = new CodeChunkingService();

        var result = service.Chunk(file);

        var firstChunkLines = result[0]
            .Content
            .Split(
                new[] { "\r\n", "\n", "\r" },
                StringSplitOptions.None);

        var secondChunkLines = result[1]
            .Content
            .Split(
                new[] { "\r\n", "\n", "\r" },
                StringSplitOptions.None);

        Assert.Equal("line 36", secondChunkLines[0]);
        Assert.Equal(
            "line 36",
            firstChunkLines[firstChunkLines.Length - 5]);

        Assert.Equal("line 40", secondChunkLines[4]);
        Assert.Equal(
            "line 40",
            firstChunkLines[firstChunkLines.Length - 1]);
    }

    [Fact]
    public void Chunk_IncrementsChunkIndex()
    {
        var content = CreateNumberedLines(120);

        var file = CreateFile(content);

        var service = new CodeChunkingService();

        var result = service.Chunk(file);

        Assert.Equal(4, result.Count);

        Assert.Equal(0, result[0].ChunkIndex);
        Assert.Equal(1, result[1].ChunkIndex);
        Assert.Equal(2, result[2].ChunkIndex);
        Assert.Equal(3, result[3].ChunkIndex);
    }

    [Fact]
    public void Chunk_PreservesFileMetadata()
    {
        var content = "line 1\nline 2\nline 3";

        var file = new SourceFileMetadata(
            "owner/repository",
            "main",
            "src/Services/OrderService.cs",
            "csharp",
            ".cs",
            3,
            content.Length,
            System.Text.Encoding.UTF8.GetByteCount(content),
            content);

        var service = new CodeChunkingService();

        var result = service.Chunk(file);

        var chunk = Assert.Single(result);

        Assert.Equal(
            file.Repository,
            chunk.Repository);

        Assert.Equal(
            file.Branch,
            chunk.Branch);

        Assert.Equal(
            file.Path,
            chunk.Path);

        Assert.Equal(
            file.Language,
            chunk.Language);
    }

    [Fact]
    public void Chunk_SupportsCustomChunkSizeAndOverlap()
    {
        var content = CreateNumberedLines(10);

        var file = CreateFile(content);

        var service = new CodeChunkingService(
            maxLines: 4,
            overlapLines: 1);

        var result = service.Chunk(file);

        Assert.Equal(3, result.Count);

        Assert.Equal(1, result[0].StartLine);
        Assert.Equal(4, result[0].EndLine);

        Assert.Equal(4, result[1].StartLine);
        Assert.Equal(7, result[1].EndLine);

        Assert.Equal(7, result[2].StartLine);
        Assert.Equal(10, result[2].EndLine);
    }

    [Fact]
    public void Constructor_ThrowsWhenMaxLinesIsZero()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new CodeChunkingService(
                maxLines: 0,
                overlapLines: 0));
    }

    [Fact]
    public void Constructor_ThrowsWhenOverlapIsNegative()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new CodeChunkingService(
                maxLines: 40,
                overlapLines: -1));
    }

    [Fact]
    public void Constructor_ThrowsWhenOverlapIsGreaterThanOrEqualToMaxLines()
    {
        Assert.Throws<ArgumentException>(
            () => new CodeChunkingService(
                maxLines: 10,
                overlapLines: 10));
    }

    private static SourceFileMetadata CreateFile(string content)
    {
        var lineCount = string.IsNullOrEmpty(content)
            ? 0
            : content.Count(character => character == '\n') +
              (content.EndsWith('\n') ? 0 : 1);

        return new SourceFileMetadata(
            "owner/repository",
            "main",
            "src/Test.cs",
            "csharp",
            ".cs",
            lineCount,
            content.Length,
            System.Text.Encoding.UTF8.GetByteCount(content),
            content);
    }

    private static string CreateNumberedLines(int count)
    {
        return string.Join(
            "\n",
            Enumerable.Range(1, count)
                .Select(number => $"line {number}"));
    }
}
