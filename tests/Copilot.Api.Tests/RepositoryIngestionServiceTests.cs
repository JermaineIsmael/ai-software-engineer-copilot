using Copilot.Api.Models.Repository;
using Copilot.Api.Services.Repository;
using Microsoft.Extensions.Logging;
using Moq;

namespace Copilot.Api.Tests;

public class RepositoryIngestionServiceTests
{
    [Fact]
    public async Task IngestAsync_ReturnsProcessedRepositorySnapshotWithMetadata()
    {
        var expectedSnapshot = new RepositorySnapshot(
            "owner/repository",
            "main",
            new List<RepositoryFile>
            {
                new(
                    "owner/repository",
                    "main",
                    "src/Program.cs",
                    "cs",
                    "Console.WriteLine(\"Hello\");")
            });

        var provider = new Mock<IRepositoryProvider>();

        provider
            .Setup(x => x.GetRepositoryAsync(
                "https://github.com/owner/repository",
                "main",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedSnapshot);

        var metadataService = new SourceFileMetadataService(
            new SourceFileClassifier());

        var logger = Mock.Of<ILogger<RepositoryIngestionService>>();

        var service = new RepositoryIngestionService(
            provider.Object,
            metadataService,
            logger);

        var result = await service.IngestAsync(
            "https://github.com/owner/repository",
            "main");

        Assert.Equal(
            expectedSnapshot.Repository,
            result.Repository);

        Assert.Equal(
            expectedSnapshot.Branch,
            result.Branch);

        Assert.Single(result.Files);

        var file = result.Files[0];

        Assert.Equal(
            "src/Program.cs",
            file.Path);

        Assert.Equal(
            "csharp",
            file.Language);

        Assert.Equal(
            ".cs",
            file.Extension);

        Assert.Equal(
            1,
            file.LineCount);

        Assert.Equal(
            "Console.WriteLine(\"Hello\");".Length,
            file.CharacterCount);

        Assert.Equal(
            System.Text.Encoding.UTF8.GetByteCount(
                "Console.WriteLine(\"Hello\");"),
            file.ByteCount);

        Assert.Equal(
            "Console.WriteLine(\"Hello\");",
            file.Content);

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
                new(
                    "owner/repository",
                    "main",
                    "src/Program.cs",
                    "cs",
                    "Console.WriteLine(\"Hello\");"),

                new(
                    "owner/repository",
                    "main",
                    "appsettings.json",
                    "json",
                    "{ \"Environment\": \"Development\" }")
            });

        var provider = new Mock<IRepositoryProvider>();

        provider
            .Setup(x => x.GetRepositoryAsync(
                "https://github.com/owner/repository",
                "main",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedSnapshot);

        var metadataService = new SourceFileMetadataService(
            new SourceFileClassifier());

        var logger = Mock.Of<ILogger<RepositoryIngestionService>>();

        var service = new RepositoryIngestionService(
            provider.Object,
            metadataService,
            logger);

        var result = await service.IngestAsync(
            "https://github.com/owner/repository",
            "main");

        Assert.Equal(2, result.Files.Count);

        Assert.Equal(
            "csharp",
            result.Files[0].Language);

        Assert.Equal(
            "json",
            result.Files[1].Language);

        Assert.Equal(
            ".cs",
            result.Files[0].Extension);

        Assert.Equal(
            ".json",
            result.Files[1].Extension);
    }

    [Fact]
    public async Task IngestAsync_ThrowsWhenRepositoryUrlIsMissing()
    {
        var provider = new Mock<IRepositoryProvider>();

        var metadataService = new SourceFileMetadataService(
            new SourceFileClassifier());

        var logger = Mock.Of<ILogger<RepositoryIngestionService>>();

        var service = new RepositoryIngestionService(
            provider.Object,
            metadataService,
            logger);

        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => service.IngestAsync("", "main"));

        Assert.Equal(
            "repositoryUrl",
            exception.ParamName);

        provider.Verify(
            x => x.GetRepositoryAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task IngestAsync_ThrowsWhenBranchIsMissing()
    {
        var provider = new Mock<IRepositoryProvider>();

        var metadataService = new SourceFileMetadataService(
            new SourceFileClassifier());

        var logger = Mock.Of<ILogger<RepositoryIngestionService>>();

        var service = new RepositoryIngestionService(
            provider.Object,
            metadataService,
            logger);

        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => service.IngestAsync(
                "https://github.com/owner/repository",
                ""));

        Assert.Equal(
            "branch",
            exception.ParamName);

        provider.Verify(
            x => x.GetRepositoryAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
