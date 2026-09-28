namespace Copilot.Api.Services.Tools;

public sealed class CopilotToolRegistry : ICopilotToolRegistry
{
    private readonly IReadOnlyDictionary<string, ICopilotTool> _tools;

    public CopilotToolRegistry(IEnumerable<ICopilotTool> tools)
    {
        ArgumentNullException.ThrowIfNull(tools);

        _tools = tools.ToDictionary(
            tool => tool.Name,
            StringComparer.OrdinalIgnoreCase);
    }

    public IReadOnlyCollection<ICopilotTool> GetTools()
    {
        return _tools.Values.ToArray();
    }

    public ICopilotTool? GetTool(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return null;
        }

        return _tools.TryGetValue(name, out var tool)
            ? tool
            : null;
    }
}
