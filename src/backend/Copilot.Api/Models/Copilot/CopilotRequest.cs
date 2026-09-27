namespace Copilot.Api.Models.Copilot;

public sealed record CopilotRequest(
    string Query,
    int Top = 5);
