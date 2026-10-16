using System.Diagnostics;
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
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
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
        Assert.Contains(
            registry.GetTools(),
            tool => tool.Name == "run_workspace_build");
        Assert.Contains(
            registry.GetTools(),
            tool => tool.Name == "run_workspace_tests");
    }

    [Fact]
    public async Task RunAsync_RequiresApprovalAndConsumesItAfterOneRun()
    {
        await using var environment = new WorkspaceTestEnvironment();
        var workspace = await environment.CreateWorkspaceAsync();
        var targetPath = Path.Combine(
            workspace.WorkingDirectory,
            "sample.csproj");
        await File.WriteAllTextAsync(targetPath, "<Project />");

        var processRunner = new FakeCommandProcessRunner(
            new CommandProcessResult(
                TimedOut: false,
                ExitCode: 0,
                DurationMilliseconds: 42,
                Output: "build succeeded",
                OutputTruncated: false));
        var approval = new BuildTestExecutionApproval();
        var service = CreateBuildTestExecutionService(
            environment.Manager,
            approval,
            processRunner);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.RunAsync(
                workspace.Id,
                BuildTestOperation.Build,
                "sample.csproj"));
        Assert.Null(processRunner.StartInfo);

        approval.SetApproved(true);
        var result = await service.RunAsync(
            workspace.Id,
            BuildTestOperation.Build,
            "sample.csproj");

        Assert.True(result.Succeeded);
        Assert.Equal(42, result.DurationMilliseconds);
        Assert.Equal("dotnet", processRunner.StartInfo!.FileName);
        Assert.Equal(
            [
                "build",
                targetPath,
                "--configuration",
                "Release",
                "--nologo"
            ],
            processRunner.StartInfo.ArgumentList);
        Assert.False(
            processRunner.StartInfo.Environment.ContainsKey(
                "AzureOpenAI__ApiKey"));
        Assert.False(
            processRunner.StartInfo.Environment.ContainsKey(
                "GitHub__Token"));

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.RunAsync(
                workspace.Id,
                BuildTestOperation.Test,
                "sample.csproj"));
    }

    [Fact]
    public async Task RunAsync_ReturnsFailureForNonZeroExitCode()
    {
        await using var environment = new WorkspaceTestEnvironment();
        var workspace = await environment.CreateWorkspaceAsync();
        await File.WriteAllTextAsync(
            Path.Combine(workspace.WorkingDirectory, "sample.csproj"),
            "<Project />");
        var processRunner = new FakeCommandProcessRunner(
            new CommandProcessResult(
                TimedOut: false,
                ExitCode: 2,
                DurationMilliseconds: 27,
                Output: "build failed",
                OutputTruncated: false));
        var approval = new BuildTestExecutionApproval();
        approval.SetApproved(true);
        var service = CreateBuildTestExecutionService(
            environment.Manager,
            approval,
            processRunner);

        var result = await service.RunAsync(
            workspace.Id,
            BuildTestOperation.Build,
            "sample.csproj");

        Assert.False(result.Succeeded);
        Assert.Equal(2, result.ExitCode);
        Assert.Equal("build failed", result.Output);
    }

    [Fact]
    public async Task RunAsync_BuildsApprovedProjectInsideWorkspace()
    {
        await using var environment = new WorkspaceTestEnvironment();
        var workspace = await environment.CreateWorkspaceAsync();
        await File.WriteAllTextAsync(
            Path.Combine(workspace.WorkingDirectory, "sample.csproj"),
            """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFramework>net8.0</TargetFramework>
              </PropertyGroup>
            </Project>
            """);
        await File.WriteAllTextAsync(
            Path.Combine(workspace.WorkingDirectory, "Sample.cs"),
            "public sealed class Sample { }");

        var approval = new BuildTestExecutionApproval();
        approval.SetApproved(true);
        var service = new BuildTestExecutionService(
            environment.Manager,
            approval,
            new BuildTestExecutionLock(),
            new CommandProcessRunner(),
            Options.Create(new BuildTestExecutionOptions()),
            new TestHostEnvironment("Development"));

        var result = await service.RunAsync(
            workspace.Id,
            BuildTestOperation.Build,
            "sample.csproj");

        Assert.True(result.Succeeded, result.Output);
        Assert.Equal(0, result.ExitCode);
        Assert.Contains("Build succeeded", result.Output);
        Assert.True(
            Directory.Exists(
                Path.Combine(
                    Directory.GetParent(workspace.WorkingDirectory)!.FullName,
                    ".copilot-execution")));
    }

    [Fact]
    public async Task RunAsync_RejectsTargetsOutsideWorkspaceAndReturnsTimeoutResult()
    {
        await using var environment = new WorkspaceTestEnvironment();
        var workspace = await environment.CreateWorkspaceAsync();
        var targetPath = Path.Combine(
            workspace.WorkingDirectory,
            "sample.sln");
        await File.WriteAllTextAsync(targetPath, string.Empty);

        var processRunner = new FakeCommandProcessRunner(
            new CommandProcessResult(
                TimedOut: true,
                ExitCode: null,
                DurationMilliseconds: 60_000,
                Output: "timed out",
                OutputTruncated: true));
        var approval = new BuildTestExecutionApproval();
        approval.SetApproved(true);
        var service = CreateBuildTestExecutionService(
            environment.Manager,
            approval,
            processRunner);

        await Assert.ThrowsAsync<ArgumentException>(
            () => service.RunAsync(
                workspace.Id,
                BuildTestOperation.Test,
                "../outside.sln"));
        Assert.Null(processRunner.StartInfo);

        var result = await service.RunAsync(
            workspace.Id,
            BuildTestOperation.Test,
            "sample.sln");

        Assert.False(result.Succeeded);
        Assert.True(result.TimedOut);
        Assert.Null(result.ExitCode);
        Assert.True(result.OutputTruncated);
        Assert.Contains("test", processRunner.StartInfo!.ArgumentList);
    }

    [Fact]
    public async Task RunAsync_IsUnavailableOutsideDevelopmentEnvironment()
    {
        await using var environment = new WorkspaceTestEnvironment();
        var workspace = await environment.CreateWorkspaceAsync();
        var targetPath = Path.Combine(
            workspace.WorkingDirectory,
            "sample.csproj");
        await File.WriteAllTextAsync(targetPath, "<Project />");
        var processRunner = new FakeCommandProcessRunner(
            new CommandProcessResult(false, 0, 1, string.Empty, false));
        var service = new BuildTestExecutionService(
            environment.Manager,
            new BuildTestExecutionApproval(),
            new BuildTestExecutionLock(),
            processRunner,
            Options.Create(new BuildTestExecutionOptions()),
            new TestHostEnvironment("Production"));

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.RunAsync(
                workspace.Id,
                BuildTestOperation.Build,
                "sample.csproj"));
        Assert.Null(processRunner.StartInfo);
    }

    [Fact]
    public async Task RunAsync_IsUnavailableWhenDisabledByConfiguration()
    {
        await using var environment = new WorkspaceTestEnvironment();
        var workspace = await environment.CreateWorkspaceAsync();
        await File.WriteAllTextAsync(
            Path.Combine(workspace.WorkingDirectory, "sample.csproj"),
            "<Project />");
        var processRunner = new FakeCommandProcessRunner(
            new CommandProcessResult(false, 0, 1, string.Empty, false));
        var service = new BuildTestExecutionService(
            environment.Manager,
            new BuildTestExecutionApproval(),
            new BuildTestExecutionLock(),
            processRunner,
            Options.Create(
                new BuildTestExecutionOptions { Enabled = false }),
            new TestHostEnvironment("Development"));

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.RunAsync(
                workspace.Id,
                BuildTestOperation.Build,
                "sample.csproj"));
        Assert.Null(processRunner.StartInfo);
    }

    [Fact]
    public async Task CommandProcessRunner_CapturesBoundedOutput()
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };
        startInfo.ArgumentList.Add("--version");

        var result = await new CommandProcessRunner().RunAsync(
            startInfo,
            TimeSpan.FromSeconds(30),
            maximumOutputCharacters: 1);

        Assert.False(result.TimedOut);
        Assert.Equal(0, result.ExitCode);
        Assert.True(result.OutputTruncated);
        Assert.Equal(1, result.Output.Length);
    }

    [Fact]
    public async Task CommandProcessRunner_WithCancelledToken_DoesNotStartProcess()
    {
        using var cancellationSource = new CancellationTokenSource();
        cancellationSource.Cancel();

        var startInfo = new ProcessStartInfo
        {
            FileName = "executable-that-must-not-start",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => new CommandProcessRunner().RunAsync(
                startInfo,
                TimeSpan.FromSeconds(30),
                maximumOutputCharacters: 100,
                cancellationSource.Token));
    }

    [Fact]
    public async Task CommandProcessRunner_WhenExecutableCannotStart_ThrowsExplicitError()
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "executable-that-does-not-exist",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => new CommandProcessRunner().RunAsync(
                startInfo,
                TimeSpan.FromSeconds(30),
                maximumOutputCharacters: 100));

        Assert.Contains("Could not start", exception.Message);
    }

    [Fact]
    public async Task BuildTestExecutionLock_RejectsConcurrentRunWithoutQueueing()
    {
        using var executionLock = new BuildTestExecutionLock();

        Assert.True(
            await executionLock.TryAcquireAsync(CancellationToken.None));
        Assert.False(
            await executionLock.TryAcquireAsync(CancellationToken.None));

        executionLock.Release();

        Assert.True(
            await executionLock.TryAcquireAsync(CancellationToken.None));
        executionLock.Release();
    }

    private static BuildTestExecutionService CreateBuildTestExecutionService(
        IWorkspaceManager workspaceManager,
        IBuildTestExecutionApproval approval,
        FakeCommandProcessRunner processRunner)
    {
        return new BuildTestExecutionService(
            workspaceManager,
            approval,
            new BuildTestExecutionLock(),
            processRunner,
            Options.Create(new BuildTestExecutionOptions()),
            new TestHostEnvironment("Development"));
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

    private sealed class FakeCommandProcessRunner : ICommandProcessRunner
    {
        private readonly CommandProcessResult _result;

        public FakeCommandProcessRunner(CommandProcessResult result)
        {
            _result = result;
        }

        public ProcessStartInfo? StartInfo { get; private set; }

        public Task<CommandProcessResult> RunAsync(
            ProcessStartInfo startInfo,
            TimeSpan timeout,
            int maximumOutputCharacters,
            CancellationToken cancellationToken = default)
        {
            StartInfo = startInfo;
            return Task.FromResult(_result);
        }
    }

    private sealed class TestHostEnvironment : IHostEnvironment
    {
        public TestHostEnvironment(string environmentName)
        {
            EnvironmentName = environmentName;
        }

        public string EnvironmentName { get; set; }

        public string ApplicationName { get; set; } = "Copilot.Api.Tests";

        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;

        public IFileProvider ContentRootFileProvider { get; set; } =
            new NullFileProvider();
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
