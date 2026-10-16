using System.Diagnostics;
using Copilot.Api.Configuration;
using Copilot.Api.Models.Workspace;
using Microsoft.Extensions.Options;

namespace Copilot.Api.Services.Workspace;

public sealed class BuildTestExecutionService : IBuildTestExecutionService
{
    private readonly IWorkspaceManager _workspaceManager;
    private readonly IBuildTestExecutionApproval _approval;
    private readonly BuildTestExecutionLock _executionLock;
    private readonly ICommandProcessRunner _processRunner;
    private readonly BuildTestExecutionOptions _options;
    private readonly IHostEnvironment _environment;

    public BuildTestExecutionService(
        IWorkspaceManager workspaceManager,
        IBuildTestExecutionApproval approval,
        BuildTestExecutionLock executionLock,
        ICommandProcessRunner processRunner,
        IOptions<BuildTestExecutionOptions> options,
        IHostEnvironment environment)
    {
        ArgumentNullException.ThrowIfNull(workspaceManager);
        ArgumentNullException.ThrowIfNull(approval);
        ArgumentNullException.ThrowIfNull(executionLock);
        ArgumentNullException.ThrowIfNull(processRunner);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(environment);

        _workspaceManager = workspaceManager;
        _approval = approval;
        _executionLock = executionLock;
        _processRunner = processRunner;
        _options = options.Value;
        _environment = environment;
    }

    public async Task<BuildTestExecutionResult> RunAsync(
        Guid workspaceId,
        BuildTestOperation operation,
        string targetPath,
        CancellationToken cancellationToken = default)
    {
        if (!_environment.IsDevelopment() || !_options.Enabled)
        {
            throw new InvalidOperationException(
                "Build and test execution is available only when enabled in the Development environment.");
        }

        if (workspaceId == Guid.Empty)
        {
            throw new ArgumentException(
                "A valid workspace ID is required.",
                nameof(workspaceId));
        }

        if (operation is not (BuildTestOperation.Build or BuildTestOperation.Test))
        {
            throw new ArgumentOutOfRangeException(
                nameof(operation),
                "Only build and test operations are supported.");
        }

        if (_options.TimeoutSeconds is <= 0 or > 3600 ||
            _options.MaximumOutputCharacters is <= 0 or > 1_048_576)
        {
            throw new InvalidOperationException(
                "Build/test timeout must be 1–3600 seconds and output limit must be 1–1048576 characters.");
        }

        await using var workspaceLease = await _workspaceManager.AcquireAsync(
            workspaceId,
            cancellationToken);

        var target = ResolveTargetPath(
            workspaceLease.Workspace.WorkingDirectory,
            targetPath);

        if (!await _executionLock.TryAcquireAsync(cancellationToken))
        {
            throw new InvalidOperationException(
                "Another build or test execution is already in progress.");
        }

        try
        {
            if (!_approval.TryConsume())
            {
                throw new InvalidOperationException(
                    "Build/test execution requires explicit user approval for this Copilot request.");
            }

            var startInfo = CreateStartInfo(
                workspaceLease.Workspace.WorkingDirectory,
                target,
                operation);
            var processResult = await _processRunner.RunAsync(
                startInfo,
                TimeSpan.FromSeconds(_options.TimeoutSeconds),
                _options.MaximumOutputCharacters,
                cancellationToken);

            return new BuildTestExecutionResult(
                workspaceId,
                operation,
                targetPath,
                Succeeded: !processResult.TimedOut &&
                    processResult.ExitCode == 0,
                processResult.TimedOut,
                processResult.ExitCode,
                processResult.DurationMilliseconds,
                processResult.Output,
                processResult.OutputTruncated);
        }
        finally
        {
            _executionLock.Release();
        }
    }

    private static string ResolveTargetPath(
        string workingDirectory,
        string targetPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(targetPath);

        var resolvedPath = WorkspacePathResolver.Resolve(
            workingDirectory,
            targetPath);
        var extension = Path.GetExtension(resolvedPath);

        if (!extension.Equals(".sln", StringComparison.OrdinalIgnoreCase) &&
            !extension.Equals(".csproj", StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException(
                "Target must be a workspace-relative .sln or .csproj file.",
                nameof(targetPath));
        }

        if (!File.Exists(resolvedPath))
        {
            throw new FileNotFoundException(
                "The requested build/test target does not exist in the workspace.",
                targetPath);
        }

        return resolvedPath;
    }

    private static ProcessStartInfo CreateStartInfo(
        string workingDirectory,
        string targetPath,
        BuildTestOperation operation)
    {
        var workspaceRoot = Directory.GetParent(workingDirectory)
            ?? throw new InvalidOperationException(
                "The workspace working directory has no parent directory.");
        var scratchDirectory = Path.Combine(
            workspaceRoot.FullName,
            ".copilot-execution");
        var profileDirectory = Path.Combine(scratchDirectory, "profile");
        var packageDirectory = Path.Combine(scratchDirectory, "packages");
        var applicationDataDirectory =
            Path.Combine(profileDirectory, "AppData", "Roaming");
        var localApplicationDataDirectory =
            Path.Combine(profileDirectory, "AppData", "Local");
        var temporaryDirectory = Path.Combine(scratchDirectory, "temp");

        Directory.CreateDirectory(packageDirectory);
        Directory.CreateDirectory(applicationDataDirectory);
        Directory.CreateDirectory(localApplicationDataDirectory);
        Directory.CreateDirectory(temporaryDirectory);

        var startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            WorkingDirectory = workingDirectory,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };
        startInfo.ArgumentList.Add(
            operation == BuildTestOperation.Build ? "build" : "test");
        startInfo.ArgumentList.Add(targetPath);
        startInfo.ArgumentList.Add("--configuration");
        startInfo.ArgumentList.Add("Release");
        startInfo.ArgumentList.Add("--nologo");

        startInfo.Environment.Clear();
        CopyEnvironmentVariable(startInfo, "PATH");
        CopyEnvironmentVariable(startInfo, "DOTNET_ROOT");
        CopyEnvironmentVariable(startInfo, "SystemRoot");
        CopyEnvironmentVariable(startInfo, "WINDIR");
        CopyEnvironmentVariable(startInfo, "ProgramFiles");
        CopyEnvironmentVariable(startInfo, "ProgramFiles(x86)");
        CopyEnvironmentVariable(startInfo, "SystemDrive");

        startInfo.Environment["HOME"] = profileDirectory;
        startInfo.Environment["USERPROFILE"] = profileDirectory;
        startInfo.Environment["APPDATA"] = applicationDataDirectory;
        startInfo.Environment["LOCALAPPDATA"] = localApplicationDataDirectory;
        startInfo.Environment["DOTNET_CLI_HOME"] =
            Path.Combine(profileDirectory, ".dotnet");
        startInfo.Environment["NUGET_PACKAGES"] = packageDirectory;
        startInfo.Environment["TEMP"] = temporaryDirectory;
        startInfo.Environment["TMP"] = temporaryDirectory;
        startInfo.Environment["TMPDIR"] = temporaryDirectory;
        startInfo.Environment["DOTNET_NOLOGO"] = "1";
        startInfo.Environment["DOTNET_CLI_TELEMETRY_OPTOUT"] = "1";
        startInfo.Environment["DOTNET_SKIP_FIRST_TIME_EXPERIENCE"] = "1";

        return startInfo;
    }

    private static void CopyEnvironmentVariable(
        ProcessStartInfo startInfo,
        string name)
    {
        var value = Environment.GetEnvironmentVariable(name);
        if (!string.IsNullOrWhiteSpace(value))
        {
            startInfo.Environment[name] = value;
        }
    }
}
