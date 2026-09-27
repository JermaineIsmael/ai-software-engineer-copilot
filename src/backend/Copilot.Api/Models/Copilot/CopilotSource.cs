namespace Copilot.Api.Models.Copilot;

public sealed record CopilotSource(
    string Repository,
    string Branch,
    string Path,
    int StartLine,
    int EndLine,
    double Score);
