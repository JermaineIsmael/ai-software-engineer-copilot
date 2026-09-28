namespace Copilot.Api.Services.Tools;

public interface ICopilotTool
{
    string Name { get; }

    string Description { get; }

    string ParametersJsonSchema { get; }

    Task<string> ExecuteAsync(
        string argumentsJson,
        CancellationToken cancellationToken = default);
}
