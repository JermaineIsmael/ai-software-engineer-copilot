using System.Diagnostics;
using Copilot.Api.Services.Workspace;

namespace Copilot.Api.Tests.Workspace;

public sealed class CommandProcessRunnerIntegrationTests
    : IClassFixture<CommandProcessRunnerFixture>
{
    private static readonly TimeSpan ProcessExitTimeout =
        TimeSpan.FromSeconds(5);
    private readonly CommandProcessRunnerFixture _fixture;

    public CommandProcessRunnerIntegrationTests(
        CommandProcessRunnerFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task RunAsync_WhenTimedOut_KillsSpawnedProcessTree()
    {
        var childProcessIdFile = _fixture.CreateProcessIdFile();
        var runTask = new CommandProcessRunner().RunAsync(
            _fixture.CreateParentStartInfo(childProcessIdFile),
            TimeSpan.FromSeconds(3),
            maximumOutputCharacters: 4096);

        var childProcessId = await WaitForProcessIdAsync(
            childProcessIdFile,
            runTask);
        var result = await runTask;

        Assert.True(result.TimedOut);
        Assert.Null(result.ExitCode);
        await AssertProcessExitedAsync(childProcessId);
    }

    [Fact]
    public async Task RunAsync_WhenCancelled_KillsSpawnedProcessTree()
    {
        var childProcessIdFile = _fixture.CreateProcessIdFile();
        using var cancellationSource = new CancellationTokenSource();
        var runTask = new CommandProcessRunner().RunAsync(
            _fixture.CreateParentStartInfo(childProcessIdFile),
            TimeSpan.FromSeconds(30),
            maximumOutputCharacters: 4096,
            cancellationSource.Token);

        var childProcessId = await WaitForProcessIdAsync(
            childProcessIdFile,
            runTask);
        cancellationSource.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => runTask);
        await AssertProcessExitedAsync(childProcessId);
    }

    private static async Task<int> WaitForProcessIdAsync(
        string processIdFile,
        Task<CommandProcessResult> runTask)
    {
        using var timeoutSource = new CancellationTokenSource(
            ProcessExitTimeout);

        while (!File.Exists(processIdFile))
        {
            if (runTask.IsCompleted)
            {
                await runTask;
                throw new InvalidOperationException(
                    "The child process did not write its process ID.");
            }

            await Task.Delay(
                TimeSpan.FromMilliseconds(25),
                timeoutSource.Token);
        }

        var processIdText = await File.ReadAllTextAsync(
            processIdFile,
            timeoutSource.Token);
        return int.Parse(processIdText);
    }

    private static async Task AssertProcessExitedAsync(int processId)
    {
        using var timeoutSource = new CancellationTokenSource(
            ProcessExitTimeout);

        while (IsProcessRunning(processId))
        {
            await Task.Delay(
                TimeSpan.FromMilliseconds(50),
                timeoutSource.Token);
        }
    }

    private static bool IsProcessRunning(int processId)
    {
        try
        {
            using var process = Process.GetProcessById(processId);
            return !process.HasExited;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }
}

public sealed class CommandProcessRunnerFixture : IAsyncLifetime
{
    private const string ChildProjectFile = """
        <Project Sdk="Microsoft.NET.Sdk">
          <PropertyGroup>
            <OutputType>Exe</OutputType>
            <TargetFramework>net8.0</TargetFramework>
            <ImplicitUsings>enable</ImplicitUsings>
            <Nullable>enable</Nullable>
          </PropertyGroup>
        </Project>
        """;

    private const string ChildProgramFile = """
        using System.Diagnostics;
        using System.Reflection;

        if (args.Length == 2 && args[0] == "child")
        {
            var processIdTemporaryFile = args[1] + ".tmp";
            await File.WriteAllTextAsync(
                processIdTemporaryFile,
                Environment.ProcessId.ToString());
            File.Move(processIdTemporaryFile, args[1]);
            await Task.Delay(Timeout.InfiniteTimeSpan);
        }
        else if (args.Length == 2 && args[0] == "parent")
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                UseShellExecute = false
            };
            startInfo.ArgumentList.Add(Assembly.GetExecutingAssembly().Location);
            startInfo.ArgumentList.Add("child");
            startInfo.ArgumentList.Add(args[1]);

            using var child = Process.Start(startInfo)
                ?? throw new InvalidOperationException(
                    "Could not start the child process.");
            await Task.Delay(Timeout.InfiniteTimeSpan);
        }
        """;

    private readonly string _workingDirectory = Path.Combine(
        Path.GetTempPath(),
        $"copilot-process-runner-{Guid.NewGuid():N}");

    public string HelperAssemblyPath => Path.Combine(
        _workingDirectory,
        "bin",
        "Release",
        "net8.0",
        "CommandProcessRunnerChild.dll");

    public async Task InitializeAsync()
    {
        Directory.CreateDirectory(_workingDirectory);
        await File.WriteAllTextAsync(
            Path.Combine(_workingDirectory, "CommandProcessRunnerChild.csproj"),
            ChildProjectFile);
        await File.WriteAllTextAsync(
            Path.Combine(_workingDirectory, "Program.cs"),
            ChildProgramFile);

        var startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            WorkingDirectory = _workingDirectory,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };
        startInfo.ArgumentList.Add("build");
        startInfo.ArgumentList.Add("CommandProcessRunnerChild.csproj");
        startInfo.ArgumentList.Add("--configuration");
        startInfo.ArgumentList.Add("Release");
        startInfo.ArgumentList.Add("--nologo");
        startInfo.ArgumentList.Add("--verbosity");
        startInfo.ArgumentList.Add("quiet");

        var buildResult = await new CommandProcessRunner().RunAsync(
            startInfo,
            TimeSpan.FromMinutes(2),
            maximumOutputCharacters: 8192);

        Assert.False(buildResult.TimedOut);
        Assert.Equal(0, buildResult.ExitCode);
        Assert.True(File.Exists(HelperAssemblyPath));
    }

    public async Task DisposeAsync()
    {
        for (var attempt = 0; attempt < 5; attempt++)
        {
            try
            {
                if (Directory.Exists(_workingDirectory))
                {
                    Directory.Delete(_workingDirectory, recursive: true);
                }

                return;
            }
            catch (Exception exception)
                when (attempt < 4 &&
                    exception is IOException or UnauthorizedAccessException)
            {
                await Task.Delay(TimeSpan.FromMilliseconds(100));
            }
        }
    }

    public string CreateProcessIdFile() => Path.Combine(
        _workingDirectory,
        $"child-{Guid.NewGuid():N}.pid");

    public ProcessStartInfo CreateParentStartInfo(string processIdFile)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            WorkingDirectory = _workingDirectory,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };
        startInfo.ArgumentList.Add(HelperAssemblyPath);
        startInfo.ArgumentList.Add("parent");
        startInfo.ArgumentList.Add(processIdFile);
        return startInfo;
    }
}
