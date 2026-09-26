using Copilot.Api.Models.Repository;
using Copilot.Api.Services.Repository;
using Microsoft.Extensions.Logging;
using Moq;

namespace Copilot.Api.Tests;

public class RepositoryIngestionServiceTests
{
    [Fact]
    public async Task IngestAsync_ReturnsRepositorySnapshot()
    {
        // Arrange
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

        var logger = Mock.Of<ILogger<RepositoryIngestionService>>();

        var service = new RepositoryIngestionService(
            provider.Object,
            logger);

        // Act
        var result = await service.IngestAsync(
            "https://github.com/owner/repository",
            "main");

        // Assert
        Assert.Equal(
            expectedSnapshot.Repository,
            result.Repository);

        Assert.Equal(
            expectedSnapshot.Branch,
            result.Branch);

        Assert.Single(result.Files);

        Assert.Equal(
            "src/Program.cs",
            result.Files[0].Path);

        provider.Verify(
            x => x.GetRepositoryAsync(
                "https://github.com/owner/repository",
                "main",
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task IngestAsync_ThrowsWhenRepositoryUrlIsMissing()
    {
        // Arrange
        var provider = new Mock<IRepositoryProvider>();

        var logger = Mock.Of<ILogger<RepositoryIngestionService>>();

        var service = new RepositoryIngestionService(
            provider.Object,
            logger);

        // Act & Assert
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
        // Arrange
        var provider = new Mock<IRepositoryProvider>();

        var logger = Mock.Of<ILogger<RepositoryIngestionService>>();

        var service = new RepositoryIngestionService(
            provider.Object,
            logger);

        // Act & Assert
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
