using Copilot.Api.Models.FileInspection;
using Copilot.Api.Services.FileInspection;
using Copilot.Api.Services.Tools;

namespace Copilot.Api.Tests.Tools;

public sealed class FileInspectionToolTests
{
    [Fact]
    public async Task ExecuteAsync_ExecutesInspectionAndReturnsJson()
    {
        var inspectionService =
            new FakeFileInspectionService();

        var tool =
            new FileInspectionTool(
                inspectionService);

        var result =
            await tool.ExecuteAsync(
                """
                {
                  "repositoryUrl": "https://github.com/example/repository",
                  "path": "src/OrderService.cs",
                  "branch": "develop"
                }
                """);

        Assert.Contains(
            "src/OrderService.cs",
            result);

        Assert.Equal(
            "https://github.com/example/repository",
            inspectionService.Request?.RepositoryUrl);

        Assert.Equal(
            "src/OrderService.cs",
            inspectionService.Request?.Path);

        Assert.Equal(
            "develop",
            inspectionService.Request?.Branch);
    }

    [Fact]
    public async Task ExecuteAsync_UsesDefaultBranch()
    {
        var inspectionService =
            new FakeFileInspectionService();

        var tool =
            new FileInspectionTool(
                inspectionService);

        await tool.ExecuteAsync(
            """
            {
              "repositoryUrl": "https://github.com/example/repository",
              "path": "src/OrderService.cs"
            }
            """);

        Assert.Equal(
            "main",
            inspectionService.Request?.Branch);
    }

    [Fact]
    public async Task ExecuteAsync_RejectsMissingPath()
    {
        var tool =
            new FileInspectionTool(
                new FakeFileInspectionService());

        await Assert.ThrowsAsync<ArgumentException>(
            () =>
                tool.ExecuteAsync(
                    """
                    {
                      "repositoryUrl": "https://github.com/example/repository"
                    }
                    """));
    }

    private sealed class FakeFileInspectionService :
        IFileInspectionService
    {
        public FileInspectionRequest? Request { get; private set; }

        public Task<FileInspectionResponse> InspectAsync(
            FileInspectionRequest request,
            CancellationToken cancellationToken = default)
        {
            Request = request;

            return Task.FromResult(
                new FileInspectionResponse(
                    request.RepositoryUrl,
                    request.Branch,
                    request.Path,
                    "csharp",
                    "public class OrderService {}"));
        }
    }
}
