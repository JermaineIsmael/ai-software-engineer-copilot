namespace Copilot.Api.Services.Workspace;

public sealed record CommandProcessResult(
    bool TimedOut,
    int? ExitCode,
    long DurationMilliseconds,
    string Output,
    bool OutputTruncated);
