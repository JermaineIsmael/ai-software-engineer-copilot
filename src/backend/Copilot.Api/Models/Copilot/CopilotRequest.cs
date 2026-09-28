namespace Copilot.Api.Models.Copilot;

public sealed record CopilotRequest(
    string Query,
    string Repository,
    string Branch = "main",
    int Top = 5);
