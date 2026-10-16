namespace Copilot.Api.Models.Workspace;

public sealed record BuildTestExecutionResult(
    Guid WorkspaceId,
    BuildTestOperation Operation,
    string TargetPath,
    bool Succeeded,
    bool TimedOut,
    int? ExitCode,
    long DurationMilliseconds,
    string Output,
    bool OutputTruncated);
