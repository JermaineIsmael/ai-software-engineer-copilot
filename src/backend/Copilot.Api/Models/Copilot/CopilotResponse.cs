namespace Copilot.Api.Models.Copilot;

public sealed record CopilotResponse(
    string Query,
    string Answer,
    IReadOnlyList<CopilotSource> Sources);
