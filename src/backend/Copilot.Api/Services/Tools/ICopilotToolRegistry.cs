namespace Copilot.Api.Services.Tools;

public interface ICopilotToolRegistry
{
    IReadOnlyCollection<ICopilotTool> GetTools();

    ICopilotTool? GetTool(string name);
}
