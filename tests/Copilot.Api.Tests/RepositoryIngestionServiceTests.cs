using Copilot.Api.Models.Repository;
using Copilot.Api.Services.Repository;
using Microsoft.Extensions.Logging;
using Moq;

namespace Copilot.Api.Tests;

public class RepositoryIngestionServiceTests
{
    [Fact]
    public async Task IngestAsync_ReturnsProcessedRepositorySnapshotWithMetadataAndChunks()
    {
        var content = "Console.WriteLine(\"Hello\");";
        var expectedSnapshot = new RepositorySnapshot(
            "owner/repository",
            "main",
            new List<RepositoryFile>
            {
                new("owner/repository", "main", "src/Program.cs", "cs", content)
            });

        var provider = new Mock<IRepositoryProvider>();
        provider
            .Setup(x => x.GetRepositoryAsync(
                "https://github.com/owner/repository",
                "main",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedSnapshot);

        var metadataService = new SourceFileMetadataService(new SourceFileClassifier());
        var chunkingService = new CodeChunkingService();
        var logger = Mock.Of<ILogger<RepositoryIngestionService>>();
        var service = new RepositoryIngestionService(provider.Object, metadataService, chunkingService, logger);

        var result = await service.IngestAsync("https://github.com/owner/repository", "main");

        Assert.Equal(expectedSnapshot.Repository, result.Repository);
        Assert.Equal(expectedSnapshot.Branch, result.Branch);
        Assert.Single(result.Files);
        Assert.Single(result.Chunks);

        var file = result.Files[0];
        Assert.Equal("src/Program.cs", file.Path);
        Assert.Equal("csharp", file.Language);
        Assert.Equal(".cs", file.Extension);
        Assert.Equal(1, file.LineCount);
        Assert.Equal(content.Length, file.CharacterCount);
        Assert.Equal(System.Text.Encoding.UTF8.GetByteCount(content), file.ByteCount);
        Assert.Equal(content, file.Content);

        var chunk = result.Chunks[0];
        Assert.Equal("owner/repository", chunk.Repository);
        Assert.Equal("main", chunk.Branch);
        Assert.Equal("src/Program.cs", chunk.Path);
        Assert.Equal("csharp", chunk.Language);
        Assert.Equal(0, chunk.ChunkIndex);
        Assert.Equal(1, chunk.StartLine);
        Assert.Equal(1, chunk.EndLine);
        Assert.Equal(content, chunk.Content);

        provider.Verify(
            x => x.GetRepositoryAsync(
                "https://github.com/owner/repository",
                "main",
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task IngestAsync_ProcessesMultipleFiles()
    {
        var expectedSnapshot = new RepositorySnapshot(
            "owner/repository",
            "main",
            new List<RepositoryFile>
            {
                new("owner/repository", "main", "src/Program.cs", "cs", "Console.WriteLine(\"Hello\");"),
                new("owner/repository", "main", "appsettings.json", "json", "{ \"Environment\": \"Development\" }")
            });

        var provider = new Mock<IRepositoryProvider>();
        provider
            .Setup(x => x.GetRepositoryAsync(
                "https://github.com/owner/repository",
                "main",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedSnapshot);

        var metadataService = new SourceFileMetadataService(new SourceFileClassifier());
        var chunkingService = new CodeChunkingService();
        var logger = Mock.Of<ILogger<RepositoryIngestionService>>();
        var service = new RepositoryIngestionService(provider.Object, metadataService, chunkingService, logger);

        var result = await service.IngestAsync("https://github.com/owner/repository", "main");

        Assert.Equal(2, result.Files.Count);
        Assert.Equal(2, result.Chunks.Count);
        Assert.Equal("csharp", result.Files[0].Language);
        Assert.Equal("json", result.Files[1].Language);
        Assert.Equal(".cs", result.Files[0].Extension);
        Assert.Equal(".json", result.Files[1].Extension);
        Assert.Equal("src/Program.cs", result.Chunks[0].Path);
        Assert.Equal("appsettings.json", result.Chunks[1].Path);
    }

    [Fact]
    public async Task IngestAsync_ThrowsWhenRepositoryUrlIsMissing()
    {
        var provider = new Mock<IRepositoryProvider>();
        var metadataService = new SourceFileMetadataService(new SourceFileClassifier());
        var chunkingService = new CodeChunkingService();
        var logger = Mock.Of<ILogger<RepositoryIngestionService>>();
        var service = new RepositoryIngestionService(provider.Object, metadataService, chunkingService, logger);

        var exception = await Assert.ThrowsAsync<ArgumentException>(() => service.IngestAsync("", "main"));

        Assert.Equal("repositoryUrl", exception.ParamName);
        provider.Verify(
            x => x.GetRepositoryAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task IngestAsync_ThrowsWhenBranchIsMissing()
    {
        var provider = new Mock<IRepositoryProvider>();
        var metadataService = new SourceFileMetadataService(new SourceFileClassifier());
        var chunkingService = new CodeChunkingService();
        var logger = Mock.Of<ILogger<RepositoryIngestionService>>();
        var service = new RepositoryIngestionService(provider.Object, metadataService, chunkingService, logger);

        var exception = await Assert.ThrowsAsync<ArgumentException>(() => service.IngestAsync("https://github.com/owner/repository", ""));

        Assert.Equal("branch", exception.ParamName);
        provider.Verify(
            x => x.GetRepositoryAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}