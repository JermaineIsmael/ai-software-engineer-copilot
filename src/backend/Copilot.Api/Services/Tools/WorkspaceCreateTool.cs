using System.Text.Json;
using Copilot.Api.Services.Workspace;

namespace Copilot.Api.Services.Tools;

public sealed class WorkspaceCreateTool : ICopilotTool
{
    private readonly IWorkspaceManager _workspaceManager;

    public WorkspaceCreateTool(IWorkspaceManager workspaceManager)
    {
        ArgumentNullException.ThrowIfNull(workspaceManager);
        _workspaceManager = workspaceManager;
    }

    public string Name => "workspace_create";

    public string Description =>
        "Create an isolated workspace containing the supported source and text files " +
        "from a GitHub repository branch. Changes are kept in the workspace and do " +
        "not modify the remote repository or the application source checkout.";

    public string ParametersJsonSchema =>
        """
        {
          "type": "object",
          "properties": {
            "repositoryUrl": {
              "type": "string",
              "description": "HTTPS URL of the GitHub repository being modified."
            },
            "branch": {
              "type": "string",
              "description": "Repository branch to use.",
              "default": "main"
            }
          },
          "required": ["repositoryUrl"],
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
            var repositoryUrl =
                WorkspaceToolJson.GetRequiredString(root, "repositoryUrl");
            var branch = WorkspaceToolJson.GetOptionalString(root, "branch")
                ?? "main";

            if (string.IsNullOrWhiteSpace(branch))
            {
                throw new ArgumentException(
                    "The 'branch' argument cannot be empty.");
            }

            var workspace = await _workspaceManager.CreateAsync(
                repositoryUrl,
                branch,
                cancellationToken);

            return JsonSerializer.Serialize(
                new
                {
                    workspaceId = workspace.Id,
                    repository = workspace.Repository.Url,
                    branch = workspace.Branch.Name,
                    baseCommitSha = workspace.BaseCommitSha,
                    status = workspace.Status
                },
                WorkspaceToolJson.Options);
        }
    }
}
