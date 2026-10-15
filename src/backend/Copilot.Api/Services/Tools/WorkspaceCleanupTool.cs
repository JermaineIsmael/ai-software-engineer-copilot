using System.Text.Json;
using Copilot.Api.Services.Workspace;

namespace Copilot.Api.Services.Tools;

public sealed class WorkspaceCleanupTool : ICopilotTool
{
    private readonly IWorkspaceManager _workspaceManager;

    public WorkspaceCleanupTool(IWorkspaceManager workspaceManager)
    {
        ArgumentNullException.ThrowIfNull(workspaceManager);
        _workspaceManager = workspaceManager;
    }

    public string Name => "workspace_cleanup";

    public string Description =>
        "Permanently remove an isolated workspace and its baseline. Do not use until " +
        "the user has received or no longer needs its diff.";

    public string ParametersJsonSchema =>
        """
        {
          "type": "object",
          "properties": {
            "workspaceId": {
              "type": "string",
              "format": "uuid",
              "description": "ID of the isolated workspace to remove."
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
            var workspace = await _workspaceManager.CleanupAsync(
                workspaceId,
                cancellationToken);

            return JsonSerializer.Serialize(
                new
                {
                    workspaceId = workspace.Id,
                    status = workspace.Status
                },
                WorkspaceToolJson.Options);
        }
    }
}
