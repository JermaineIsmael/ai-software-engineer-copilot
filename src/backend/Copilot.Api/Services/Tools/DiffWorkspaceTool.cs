using System.Text.Json;
using Copilot.Api.Services.Workspace;

namespace Copilot.Api.Services.Tools;

public sealed class DiffWorkspaceTool : ICopilotTool
{
    private readonly IDiffService _diffService;

    public DiffWorkspaceTool(IDiffService diffService)
    {
        ArgumentNullException.ThrowIfNull(diffService);
        _diffService = diffService;
    }

    public string Name => "diff_workspace";

    public string Description =>
        "Return a unified text diff between an isolated workspace and the exact " +
        "repository snapshot from which it was created.";

    public string ParametersJsonSchema =>
        """
        {
          "type": "object",
          "properties": {
            "workspaceId": {
              "type": "string",
              "format": "uuid",
              "description": "ID returned by workspace_create."
            }
          },
          "required": ["workspaceId"],
          "additionalProperties": false
        }
        """;

    public async Task<string> ExecuteAsync(
        string argumentsJson,
        CancellationToken cancellationToken = default)
    {
        var root = WorkspaceToolJson.ParseObject(
            argumentsJson,
            out var document);

        using (document)
        {
            var workspaceId =
                WorkspaceToolJson.GetRequiredGuid(root, "workspaceId");
            var result = await _diffService.GetDiffAsync(
                workspaceId,
                cancellationToken);

            return JsonSerializer.Serialize(
                result,
                WorkspaceToolJson.Options);
        }
    }
}
