using System.Text.Json;
using Copilot.Api.Models.Workspace;
using Copilot.Api.Services.Workspace;

namespace Copilot.Api.Services.Tools;

public abstract class BuildTestExecutionToolBase : ICopilotTool,
    IApprovalRequiredCopilotTool
{
    private readonly IBuildTestExecutionService _executionService;

    protected BuildTestExecutionToolBase(
        IBuildTestExecutionService executionService)
    {
        ArgumentNullException.ThrowIfNull(executionService);
        _executionService = executionService;
    }

    public abstract string Name { get; }

    public abstract BuildTestOperation Operation { get; }

    public abstract string Description { get; }

    public string ParametersJsonSchema =>
        """
        {
          "type": "object",
          "properties": {
            "workspaceId": {
              "type": "string",
              "format": "uuid",
              "description": "ID of the trusted local workspace. This is not an OS sandbox."
            },
            "targetPath": {
              "type": "string",
              "description": "Workspace-relative .sln or .csproj file using '/' separators."
            }
          },
          "required": ["workspaceId", "targetPath"],
          "additionalProperties": false
        }
        """;

    public async Task<string> ExecuteAsync(
        string argumentsJson,
        CancellationToken cancellationToken = default)
    {
        using var document = JsonDocument.Parse(argumentsJson);
        var root = document.RootElement;

        if (root.ValueKind != JsonValueKind.Object)
        {
            throw new ArgumentException(
                "Tool arguments must be a JSON object.");
        }

        var workspaceId =
            WorkspaceToolJson.GetRequiredGuid(root, "workspaceId");
        var targetPath =
            WorkspaceToolJson.GetRequiredString(root, "targetPath");

        try
        {
            var result = await _executionService.RunAsync(
                workspaceId,
                Operation,
                targetPath,
                cancellationToken);

            return JsonSerializer.Serialize(
                result,
                WorkspaceToolJson.Options);
        }
        catch (Exception exception)
            when (exception is ArgumentException or
                InvalidOperationException or
                IOException or
                UnauthorizedAccessException or
                KeyNotFoundException)
        {
            return JsonSerializer.Serialize(
                new
                {
                    workspaceId,
                    operation = Operation,
                    targetPath,
                    succeeded = false,
                    error = exception.Message
                },
                WorkspaceToolJson.Options);
        }
    }
}
