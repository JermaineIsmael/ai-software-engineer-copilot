using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Copilot.Api.Configuration;
using Copilot.Api.Models.Repository;
using Copilot.Api.Models.Workspace;
using Copilot.Api.Services.Repository;
using Copilot.Api.Services.Tools;
using Copilot.Api.Services.Workspace;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using WorkspaceModel = Copilot.Api.Models.Workspace.Workspace;

namespace Copilot.Api.Tests.Workspace;

public sealed class WorkspaceWorkflowTests
{
    [Fact]
    public async Task CreateAsync_ImportsSnapshotAndRecordsSourceCommit()
    {
        await using var environment = new WorkspaceTestEnvironment();

        var workspace = await environment.Manager.CreateAsync(
            "https://github.com/example/repository",
            "feature/change");

        Assert.Equal(WorkspaceStatus.Ready, workspace.Status);
        Assert.Equal(
            "0123456789abcdef0123456789abcdef01234567",
            workspace.BaseCommitSha);
        Assert.Equal(workspace.BaseCommitSha, workspace.Branch.CommitSha);
        Assert.Equal("main", workspace.Repository.DefaultBranch);
        Assert.Equal(
            "original\n",
            await File.ReadAllTextAsync(
                Path.Combine(workspace.WorkingDirectory, "src", "sample.txt")));
        Assert.Equal(
            "original\n",
            await File.ReadAllTextAsync(
                Path.Combine(
                    Path.GetDirectoryName(workspace.WorkingDirectory)!,
                    ".copilot-baseline",
                    "src",
                    "sample.txt")));

        var loadedWorkspace = await environment.Manager.GetAsync(workspace.Id);
        Assert.Equal(workspace, loadedWorkspace);
    }

    [Fact]
    public async Task CreateAsync_WhenSnapshotContainsTraversalPath_RemovesPartialWorkspace()
    {
        await using var environment = new WorkspaceTestEnvironment(
            CreateSnapshot(
                new RepositoryFile(
                    "example/repository",
                    "main",
                    "../escape.txt",
                    "text",
                    "unsafe")));

        await Assert.ThrowsAsync<ArgumentException>(
            () => environment.Manager.CreateAsync(
                "https://github.com/example/repository",
                "main"));

        Assert.Empty(Directory.EnumerateDirectories(environment.RootDirectory));
        Assert.False(
            File.Exists(
                Path.Combine(
                    Directory.GetParent(environment.RootDirectory)!.FullName,
                    "escape.txt")));
    }

    [Fact]
    public async Task ApplyAsync_AppliesCreateUpdateAndDeleteAndDiffReportsExactFiles()
    {
        await using var environment = new WorkspaceTestEnvironment();
        var workspace = await environment.CreateWorkspaceAsync();
        var outsideFile = Path.Combine(
            Directory.GetParent(environment.RootDirectory)!.FullName,
            "outside-workspace.txt");
        await File.WriteAllTextAsync(outsideFile, "unchanged");

        var patchService = environment.CreatePatchService();
        var patch = new Patch(
        [
            new FileModification(
                "src/sample.txt",
                FileOperation.Update,
                "updated\n",
                Hash("original\n")),
            new FileModification(
                "docs/new.txt",
                FileOperation.Create,
                "new file\n"),
            new FileModification(
                "delete-me.txt",
                FileOperation.Delete)
        ]);

        var result = await patchService.ApplyAsync(workspace.Id, patch);
        var diff = await environment.CreateDiffService()
            .GetDiffAsync(workspace.Id);

        Assert.Equal(3, result.Files.Count);
        Assert.Equal("updated\n", await File.ReadAllTextAsync(
            Path.Combine(workspace.WorkingDirectory, "src", "sample.txt")));
        Assert.False(File.Exists(
            Path.Combine(workspace.WorkingDirectory, "delete-me.txt")));
        Assert.Equal("unchanged", await File.ReadAllTextAsync(outsideFile));
        Assert.Equal(3, diff.Files.Count);
        Assert.Contains(
            diff.Files,
            file => file.Path == "src/sample.txt" &&
                file.Operation == FileOperation.Update &&
                file.UnifiedDiff.Contains("-original", StringComparison.Ordinal) &&
                file.UnifiedDiff.Contains("+updated", StringComparison.Ordinal));
        Assert.Contains(
            diff.Files,
            file => file.Path == "docs/new.txt" &&
                file.Operation == FileOperation.Create);
        Assert.Contains(
            diff.Files,
            file => file.Path == "delete-me.txt" &&
                file.Operation == FileOperation.Delete);
    }

    [Fact]
    public async Task ApplyAsync_RejectsTraversalWithoutWritingOutsideWorkspace()
    {
        await using var environment = new WorkspaceTestEnvironment();
        var workspace = await environment.CreateWorkspaceAsync();
        var outsidePath = Path.Combine(
            Directory.GetParent(environment.RootDirectory)!.FullName,
            "escape-from-patch.txt");

        await Assert.ThrowsAsync<ArgumentException>(
            () => environment.CreatePatchService().ApplyAsync(
                workspace.Id,
                new Patch(
                [
                    new FileModification(
                        "../escape-from-patch.txt",
                        FileOperation.Create,
                        "unsafe")
                ])));

        Assert.False(File.Exists(outsidePath));
        Assert.Equal(WorkspaceStatus.Ready,
            (await environment.Manager.GetAsync(workspace.Id)).Status);
    }

    [Fact]
    public async Task ApplyAsync_WhenExpectedHashIsStale_LeavesFileUnchanged()
    {
        await using var environment = new WorkspaceTestEnvironment();
        var workspace = await environment.CreateWorkspaceAsync();

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => environment.CreatePatchService().ApplyAsync(
                workspace.Id,
                new Patch(
                [
                    new FileModification(
                        "src/sample.txt",
                        FileOperation.Update,
                        "updated\n",
                        Hash("stale\n"))
                ])));

        Assert.Equal(
            "original\n",
            await File.ReadAllTextAsync(
                Path.Combine(workspace.WorkingDirectory, "src", "sample.txt")));
    }

    [Fact]
    public async Task ApplyAsync_WhenWorkspaceFileDivergedFromBaseline_RequiresCurrentHash()
    {
        await using var environment = new WorkspaceTestEnvironment();
        var workspace = await environment.CreateWorkspaceAsync();
        var path = Path.Combine(
            workspace.WorkingDirectory,
            "src",
            "sample.txt");
        await File.WriteAllTextAsync(path, "external edit\n");

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => environment.CreatePatchService().ApplyAsync(
                workspace.Id,
                new Patch(
                [
                    new FileModification(
                        "src/sample.txt",
                        FileOperation.Update,
                        "copilot edit\n")
                ])));

        Assert.Equal("external edit\n", await File.ReadAllTextAsync(path));

        var result = await environment.CreatePatchService().ApplyAsync(
            workspace.Id,
            new Patch(
            [
                new FileModification(
                    "src/sample.txt",
                    FileOperation.Update,
                    "copilot edit\n",
                    Hash("external edit\n"))
            ]));

        Assert.True(result.Success);
        Assert.Equal("copilot edit\n", await File.ReadAllTextAsync(path));
    }

    [Fact]
    public async Task ApplyAsync_WhenAnyOperationIsInvalid_AppliesNoneOfThePatch()
    {
        await using var environment = new WorkspaceTestEnvironment();
        var workspace = await environment.CreateWorkspaceAsync();

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => environment.CreatePatchService().ApplyAsync(
                workspace.Id,
                new Patch(
                [
                    new FileModification(
                        "src/sample.txt",
                        FileOperation.Update,
                        "updated\n"),
                    new FileModification(
                        "missing.txt",
                        FileOperation.Delete)
                ])));

        Assert.Equal(
            "original\n",
            await File.ReadAllTextAsync(
                Path.Combine(workspace.WorkingDirectory, "src", "sample.txt")));
    }

    [Fact]
    public async Task ApplyAsync_WhenAWriteFails_RollsBackEarlierOperations()
    {
        await using var environment = new WorkspaceTestEnvironment();
        var workspace = await environment.CreateWorkspaceAsync();
        var service = environment.CreatePatchService();

        await Assert.ThrowsAnyAsync<IOException>(
                () => service.ApplyAsync(
                    workspace.Id,
                    new Patch(
                    [
                        new FileModification(
                            "nested/new.txt",
                            FileOperation.Create,
                            "temporary"),
                        new FileModification(
                            "nested",
                            FileOperation.Create,
                            "cannot replace directory")
                    ])));

        Assert.False(
                File.Exists(
                    Path.Combine(
                        workspace.WorkingDirectory,
                        "nested",
                        "new.txt")));
        Assert.Equal(
                WorkspaceStatus.Ready,
                (await environment.Manager.GetAsync(workspace.Id)).Status);
    }

    [Fact]
    public async Task GetDiffAsync_WhenWorkspaceIsUnchanged_ReturnsNoFiles()
    {
        await using var environment = new WorkspaceTestEnvironment();
        var workspace = await environment.CreateWorkspaceAsync();

        var result = await environment.CreateDiffService()
            .GetDiffAsync(workspace.Id);

        Assert.False(result.HasChanges);
        Assert.Empty(result.Files);
    }

    [Fact]
    public async Task GetDiffAsync_WhenOnlyFinalNewlineChanges_ReportsTheChange()
    {
        await using var environment = new WorkspaceTestEnvironment();
        var workspace = await environment.CreateWorkspaceAsync();

        await File.WriteAllTextAsync(
            Path.Combine(workspace.WorkingDirectory, "src", "sample.txt"),
            "original");

        var diff = await environment.CreateDiffService()
            .GetDiffAsync(workspace.Id);

        var fileDiff = Assert.Single(diff.Files);
        Assert.Contains(
            "\\ No newline at end of file",
            fileDiff.UnifiedDiff);
        Assert.Contains("-original", fileDiff.UnifiedDiff);
        Assert.Contains("+original", fileDiff.UnifiedDiff);
    }

    [Fact]
    public async Task CleanupAsync_RemovesWorkspaceAndItsBaseline()
    {
        await using var environment = new WorkspaceTestEnvironment();
        var workspace = await environment.CreateWorkspaceAsync();
        var root = Path.GetDirectoryName(workspace.WorkingDirectory)!;

        var cleaned = await environment.Manager.CleanupAsync(workspace.Id);

        Assert.Equal(WorkspaceStatus.Cleaned, cleaned.Status);
        Assert.False(Directory.Exists(root));
        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => environment.Manager.GetAsync(workspace.Id));
    }

    [Fact]
    public async Task WorkspaceTools_ExecuteCreateModifyDiffAndCleanupWorkflow()
    {
        await using var environment = new WorkspaceTestEnvironment();
        var createTool = new WorkspaceCreateTool(environment.Manager);
        var createResponse = await createTool.ExecuteAsync(
            """
            {
              "repositoryUrl": "https://github.com/example/repository",
              "branch": "main"
            }
            """);
        using var createDocument = JsonDocument.Parse(createResponse);
        var workspaceId = createDocument.RootElement
            .GetProperty("workspaceId")
            .GetGuid();

        var modifyTool = new ModifyCodeTool(
            environment.CreatePatchService());
        await modifyTool.ExecuteAsync(
            $$"""
            {
              "workspaceId": "{{workspaceId}}",
              "files": [
                {
                  "path": "src/sample.txt",
                  "operation": "Update",
                  "content": "tool change\n"
                }
              ]
            }
            """);

        var diffTool = new DiffWorkspaceTool(
            environment.CreateDiffService());
        var diffResponse = await diffTool.ExecuteAsync(
            $$"""
            {
              "workspaceId": "{{workspaceId}}"
            }
            """);
        using var diffDocument = JsonDocument.Parse(diffResponse);

        Assert.Contains(
            "tool change",
            diffDocument.RootElement
                .GetProperty("files")[0]
                .GetProperty("unifiedDiff")
                .GetString());
        Assert.Equal(
            "tool change\n",
            await File.ReadAllTextAsync(
                Path.Combine(
                    (await environment.Manager.GetAsync(workspaceId))
                        .WorkingDirectory,
                    "src",
                    "sample.txt")));

        var cleanupTool = new WorkspaceCleanupTool(environment.Manager);
        var cleanupResult = await cleanupTool.ExecuteAsync(
            $$"""
            {
              "workspaceId": "{{workspaceId}}"
            }
            """);

        Assert.Contains("Cleaned", cleanupResult);
    }

    [Fact]
    public void CopilotToolRegistry_ContainsWorkspaceModificationTools()
    {
        using var factory = new WorkspaceApiFactory();
        using var scope = factory.Services.CreateScope();
        var registry = scope.ServiceProvider
            .GetRequiredService<ICopilotToolRegistry>();

        Assert.Contains(
            registry.GetTools(),
            tool => tool.Name == "workspace_create");
        Assert.Contains(
            registry.GetTools(),
            tool => tool.Name == "modify_code");
        Assert.Contains(
            registry.GetTools(),
            tool => tool.Name == "diff_workspace");
        Assert.Contains(
            registry.GetTools(),
            tool => tool.Name == "workspace_cleanup");
    }

    private static RepositorySnapshot CreateSnapshot(
        params RepositoryFile[] files)
    {
        return new RepositorySnapshot(
            "example/repository",
            "main",
            files)
        {
            CommitSha = "0123456789abcdef0123456789abcdef01234567",
            DefaultBranch = "main"
        };
    }

    private static string Hash(string content)
    {
        return Convert.ToHexString(
                SHA256.HashData(Encoding.UTF8.GetBytes(content)))
            .ToLowerInvariant();
    }

    private sealed class WorkspaceTestEnvironment : IAsyncDisposable
    {
        private readonly FakeRepositoryProvider _repositoryProvider;

        public WorkspaceTestEnvironment(
            RepositorySnapshot? snapshot = null)
        {
            RootDirectory = Path.Combine(
                Path.GetTempPath(),
                $"copilot-workspace-tests-{Guid.NewGuid():N}");
            _repositoryProvider = new FakeRepositoryProvider(
                snapshot ?? CreateSnapshot(
                    new RepositoryFile(
                        "example/repository",
                        "main",
                        "src/sample.txt",
                        "text",
                        "original\n"),
                    new RepositoryFile(
                        "example/repository",
                        "main",
                        "delete-me.txt",
                        "text",
                        "remove me\n")));
            Manager = new WorkspaceManager(
                _repositoryProvider,
                Options.Create(
                    new WorkspaceOptions
                    {
                        RootDirectory = RootDirectory
                    }),
                new WorkspaceOperationLock(),
                NullLogger<WorkspaceManager>.Instance);
        }

        public string RootDirectory { get; }

        public WorkspaceManager Manager { get; }

        public Task<WorkspaceModel> CreateWorkspaceAsync()
        {
            return Manager.CreateAsync(
                "https://github.com/example/repository",
                "main");
        }

        public PatchApplicationService CreatePatchService()
        {
            return new PatchApplicationService(
                Manager,
                Options.Create(new WorkspaceOptions()),
                NullLogger<PatchApplicationService>.Instance);
        }

        public DiffService CreateDiffService()
        {
            return new DiffService(Manager);
        }

        public ValueTask DisposeAsync()
        {
            if (Directory.Exists(RootDirectory))
            {
                Directory.Delete(RootDirectory, recursive: true);
            }

            return ValueTask.CompletedTask;
        }
    }

    private sealed class FakeRepositoryProvider : IRepositoryProvider
    {
        private readonly RepositorySnapshot _snapshot;

        public FakeRepositoryProvider(RepositorySnapshot snapshot)
        {
            _snapshot = snapshot;
        }

        public Task<RepositorySnapshot> GetRepositoryAsync(
            string repositoryUrl,
            string branch,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(
                _snapshot with
                {
                    Branch = branch,
                    Files = _snapshot.Files
                        .Select(file => file with { Branch = branch })
                        .ToArray()
                });
        }

        public Task<RepositoryFile> GetFileAsync(
            string repositoryUrl,
            string path,
            string branch,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }
    }

    private sealed class WorkspaceApiFactory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureAppConfiguration(
                (_, configuration) =>
                    configuration.AddInMemoryCollection(
                        new Dictionary<string, string?>
                        {
                            ["AzureOpenAI:Endpoint"] =
                                "https://example.openai.azure.com/",
                            ["AzureOpenAI:ApiKey"] = "test-placeholder",
                            ["AzureOpenAI:EmbeddingDeploymentName"] = "test-embedding",
                            ["AzureSearch:Endpoint"] =
                                "https://example.search.windows.net/",
                            ["AzureSearch:ApiKey"] = "test-placeholder",
                            ["AzureSearch:IndexName"] = "test-index"
                        }));
        }
    }
}
