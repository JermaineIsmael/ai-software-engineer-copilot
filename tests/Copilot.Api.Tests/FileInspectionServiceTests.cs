using Copilot.Api.Models.FileInspection;
using Copilot.Api.Models.Repository;
using Copilot.Api.Services.FileInspection;
using Copilot.Api.Services.Repository;

namespace Copilot.Api.Tests;

public sealed class FileInspectionServiceTests
{
    [Fact]
    public async Task InspectAsync_NullRequest_ThrowsArgumentNullException()
    {
        var provider = new FakeRepositoryProvider();
        var service = new FileInspectionService(provider);

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => service.InspectAsync(null!));
    }

    [Fact]
    public async Task InspectAsync_EmptyRepositoryUrl_ThrowsArgumentException()
    {
        var provider = new FakeRepositoryProvider();
        var service = new FileInspectionService(provider);

        var request = new FileInspectionRequest(
            "",
            "src/Test.cs");

        await Assert.ThrowsAsync<ArgumentException>(
            () => service.InspectAsync(request));
    }

    [Fact]
    public async Task InspectAsync_EmptyPath_ThrowsArgumentException()
    {
        var provider = new FakeRepositoryProvider();
        var service = new FileInspectionService(provider);

        var request = new FileInspectionRequest(
            "https://github.com/test/repository",
            "");

        await Assert.ThrowsAsync<ArgumentException>(
            () => service.InspectAsync(request));
    }

    [Fact]
    public async Task InspectAsync_EmptyBranch_ThrowsArgumentException()
    {
        var provider = new FakeRepositoryProvider();
        var service = new FileInspectionService(provider);

        var request = new FileInspectionRequest(
            "https://github.com/test/repository",
            "src/Test.cs",
            "");

        await Assert.ThrowsAsync<ArgumentException>(
            () => service.InspectAsync(request));
    }

    [Fact]
    public async Task InspectAsync_CancellationRequested_ThrowsOperationCanceledException()
    {
        var provider = new FakeRepositoryProvider();
        var service = new FileInspectionService(provider);

        using var cancellationTokenSource =
            new CancellationTokenSource();

        cancellationTokenSource.Cancel();

        var request = new FileInspectionRequest(
            "https://github.com/test/repository",
            "src/Test.cs");

        await Assert.ThrowsAsync<OperationCanceledException>(
            () => service.InspectAsync(
                request,
                cancellationTokenSource.Token));
    }

    [Fact]
    public async Task InspectAsync_RequestsSingleFileAndMapsResponse()
    {
        var provider = new FakeRepositoryProvider
        {
            File = new RepositoryFile(
                "test/repository",
                "develop",
                "src/Services/TestService.cs",
                "csharp",
                "public class TestService { }")
        };

        var service = new FileInspectionService(provider);

        var request = new FileInspectionRequest(
            "https://github.com/test/repository",
            "src/Services/TestService.cs",
            "develop");

        var response = await service.InspectAsync(request);

        Assert.Equal("test/repository", response.Repository);
        Assert.Equal("develop", response.Branch);
        Assert.Equal(
            "src/Services/TestService.cs",
            response.Path);
        Assert.Equal("csharp", response.Language);
        Assert.Equal(
            "public class TestService { }",
            response.Content);

        Assert.Equal(
            "https://github.com/test/repository",
            provider.RepositoryUrl);

        Assert.Equal(
            "src/Services/TestService.cs",
            provider.Path);

        Assert.Equal(
            "develop",
            provider.Branch);

        Assert.Equal(1, provider.GetFileCallCount);
    }

    private sealed class FakeRepositoryProvider : IRepositoryProvider
    {
        public RepositoryFile File { get; set; } =
            new RepositoryFile(
                "test/repository",
                "main",
                "src/Test.cs",
                "csharp",
                "test content");

        public string? RepositoryUrl { get; private set; }

        public string? Path { get; private set; }

        public string? Branch { get; private set; }

        public int GetFileCallCount { get; private set; }

        public Task<RepositorySnapshot> GetRepositoryAsync(
            string repositoryUrl,
            string branch,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                new RepositorySnapshot(
                    "test/repository",
                    branch,
                    Array.Empty<RepositoryFile>()));
        }

        public Task<RepositoryFile> GetFileAsync(
            string repositoryUrl,
            string path,
            string branch,
            CancellationToken cancellationToken = default)
        {
            RepositoryUrl = repositoryUrl;
            Path = path;
            Branch = branch;
            GetFileCallCount++;

            return Task.FromResult(File);
        }
    }
}
