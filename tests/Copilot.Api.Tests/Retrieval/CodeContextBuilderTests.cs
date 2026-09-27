using Copilot.Api.Configuration;
using Copilot.Api.Models.Retrieval;
using Copilot.Api.Services.Retrieval;
using Microsoft.Extensions.Options;

namespace Copilot.Api.Tests.Retrieval;

public sealed class CodeContextBuilderTests
{
    [Fact]
    public void Build_ShouldReturnEmptyContextForEmptyResults()
    {
        var builder = CreateBuilder();

        var context = builder.Build(
            Array.Empty<RetrievedCodeChunk>());

        Assert.Empty(context.Text);
        Assert.Empty(context.Chunks);
    }

    [Fact]
    public void Build_ShouldIncludeCodeMetadata()
    {
        var builder = CreateBuilder();

        var chunk = CreateChunk(
            id: "chunk-1",
            repository: "sample-repo",
            branch: "main",
            path: "src/Services/TestService.cs",
            language: "csharp",
            startLine: 10,
            endLine: 25,
            content: "public class TestService {}",
            score: 0.95);

        var context = builder.Build(new[] { chunk });

        Assert.Contains("Repository: sample-repo", context.Text);
        Assert.Contains("Branch: main", context.Text);
        Assert.Contains(
            "File: src/Services/TestService.cs",
            context.Text);
        Assert.Contains("Language: csharp", context.Text);
        Assert.Contains("Lines: 10-25", context.Text);
        Assert.Contains(
            "public class TestService {}",
            context.Text);
    }

    [Fact]
    public void Build_ShouldOrderChunksByScore()
    {
        var builder = CreateBuilder();

        var chunks = new[]
        {
            CreateChunk("low", "b.cs", 0.2),
            CreateChunk("high", "c.cs", 0.9),
            CreateChunk("medium", "a.cs", 0.5)
        };

        var context = builder.Build(chunks);

        Assert.Equal(
            new[] { "high", "medium", "low" },
            context.Chunks.Select(chunk => chunk.Id));
    }

    [Fact]
    public void Build_ShouldUsePathAsTieBreaker()
    {
        var builder = CreateBuilder();

        var chunks = new[]
        {
            CreateChunk("b", "z.cs", 0.8),
            CreateChunk("a", "a.cs", 0.8),
            CreateChunk("c", "m.cs", 0.8)
        };

        var context = builder.Build(chunks);

        Assert.Equal(
            new[] { "a", "c", "b" },
            context.Chunks.Select(chunk => chunk.Id));
    }

    [Fact]
    public void Build_ShouldUseChunkIndexAsSecondTieBreaker()
    {
        var builder = CreateBuilder();

        var chunks = new[]
        {
            CreateChunk("chunk2", "file.cs", 0.8, 2),
            CreateChunk("chunk0", "file.cs", 0.8, 0),
            CreateChunk("chunk1", "file.cs", 0.8, 1)
        };

        var context = builder.Build(chunks);

        Assert.Equal(
            new[] { "chunk0", "chunk1", "chunk2" },
            context.Chunks.Select(chunk => chunk.Id));
    }

    [Fact]
    public void Build_ShouldRemoveDuplicateIds()
    {
        var builder = CreateBuilder();

        var chunks = new[]
        {
            CreateChunk("duplicate", "a.cs", 0.9),
            CreateChunk("duplicate", "a.cs", 0.8),
            CreateChunk("unique", "b.cs", 0.7)
        };

        var context = builder.Build(chunks);

        Assert.Equal(2, context.Chunks.Count);
        Assert.Equal(
            new[] { "duplicate", "unique" },
            context.Chunks.Select(chunk => chunk.Id));
    }

    [Fact]
    public void Build_ShouldRespectMaximumChunks()
    {
        var builder = CreateBuilder(maxChunks: 2);

        var chunks = new[]
        {
            CreateChunk("one", "one.cs", 0.9),
            CreateChunk("two", "two.cs", 0.8),
            CreateChunk("three", "three.cs", 0.7)
        };

        var context = builder.Build(chunks);

        Assert.Equal(2, context.Chunks.Count);
    }

    [Fact]
    public void Build_ShouldRespectMaximumCharacters()
    {
        var builder = CreateBuilder(maxCharacters: 200);

        var chunks = new[]
        {
            CreateChunk(
                "one",
                "one.cs",
                0.9,
                content: new string('x', 500))
        };

        var context = builder.Build(chunks);

        Assert.True(context.Text.Length <= 200);
    }

    [Fact]
    public void Build_ShouldRejectNullChunks()
    {
        var builder = CreateBuilder();

        Assert.Throws<ArgumentNullException>(
            () => builder.Build(null!));
    }

    [Fact]
    public void Constructor_ShouldRejectInvalidMaxChunks()
    {
        var options = Options.Create(
            new RetrievalContextOptions
            {
                MaxChunks = 0,
                MaxCharacters = 100
            });

        Assert.Throws<ArgumentOutOfRangeException>(
            () => new CodeContextBuilder(options));
    }

    [Fact]
    public void Constructor_ShouldRejectInvalidMaxCharacters()
    {
        var options = Options.Create(
            new RetrievalContextOptions
            {
                MaxChunks = 5,
                MaxCharacters = 0
            });

        Assert.Throws<ArgumentOutOfRangeException>(
            () => new CodeContextBuilder(options));
    }

    private static CodeContextBuilder CreateBuilder(
        int maxChunks = 8,
        int maxCharacters = 12000)
    {
        var options = Options.Create(
            new RetrievalContextOptions
            {
                MaxChunks = maxChunks,
                MaxCharacters = maxCharacters
            });

        return new CodeContextBuilder(options);
    }

    private static RetrievedCodeChunk CreateChunk(
        string id,
        string path = "file.cs",
        double score = 0.8,
        int chunkIndex = 0,
        string repository = "test-repository",
        string branch = "main",
        string language = "csharp",
        int startLine = 1,
        int endLine = 10,
        string content = "test content")
    {
        return new RetrievedCodeChunk(
            id,
            repository,
            branch,
            path,
            language,
            chunkIndex,
            startLine,
            endLine,
            content,
            score);
    }
}
