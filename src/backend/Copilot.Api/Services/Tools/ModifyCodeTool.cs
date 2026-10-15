using System.Text.Json;
using Copilot.Api.Models.Workspace;
using Copilot.Api.Services.Workspace;

namespace Copilot.Api.Services.Tools;

public sealed class ModifyCodeTool : ICopilotTool
{
    private readonly IPatchApplicationService _patchApplicationService;

    public ModifyCodeTool(IPatchApplicationService patchApplicationService)
    {
        ArgumentNullException.ThrowIfNull(patchApplicationService);
        _patchApplicationService = patchApplicationService;
    }

    public string Name => "modify_code";

    public string Description =>
        "Apply a validated create, update, or delete patch to an existing isolated " +
        "workspace. This never changes the original repository or application checkout.";

    public string ParametersJsonSchema =>
        """
        {
          "type": "object",
          "properties": {
            "workspaceId": {
              "type": "string",
              "format": "uuid",
              "description": "ID returned by workspace_create."
            },
            "files": {
              "type": "array",
              "minItems": 1,
              "items": {
                "type": "object",
                "properties": {
                  "path": {
                    "type": "string",
                    "description": "Relative repository path using '/' separators."
                  },
                  "operation": {
                    "type": "string",
                    "enum": ["Create", "Update", "Delete"]
                  },
                  "content": {
                    "type": "string",
                    "description": "Complete UTF-8 file content for Create or Update."
                  },
                  "expectedContentHash": {
                    "type": "string",
                    "description": "Optional SHA-256 of current content for stale-change protection."
                  }
                },
                "required": ["path", "operation"],
                "additionalProperties": false
              }
            }
          },
          "required": ["workspaceId", "files"],
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

            if (!root.TryGetProperty("files", out var filesElement) ||
                filesElement.ValueKind != JsonValueKind.Array)
            {
                throw new ArgumentException(
                    "The 'files' argument must be an array.");
            }

            var files = new List<FileModification>();

            foreach (var fileElement in filesElement.EnumerateArray())
            {
                if (fileElement.ValueKind != JsonValueKind.Object)
                {
                    throw new ArgumentException(
                        "Each file operation must be a JSON object.");
                }

                var path =
                    WorkspaceToolJson.GetRequiredString(fileElement, "path");
                var operationText =
                    WorkspaceToolJson.GetRequiredString(fileElement, "operation");

                if (!Enum.TryParse<FileOperation>(
                        operationText,
                        ignoreCase: true,
                        out var operation) ||
                    !Enum.IsDefined(operation))
                {
                    throw new ArgumentException(
                        $"Unsupported file operation '{operationText}'.");
                }

                files.Add(
                    new FileModification(
                        path,
                        operation,
                        WorkspaceToolJson.GetOptionalString(
                            fileElement,
                            "content"),
                        WorkspaceToolJson.GetOptionalString(
                            fileElement,
                            "expectedContentHash")));
            }

            try
            {
                var result = await _patchApplicationService.ApplyAsync(
                    workspaceId,
                    new Patch(files),
                    cancellationToken);

                return JsonSerializer.Serialize(
                    result,
                    WorkspaceToolJson.Options);
            }
            catch (Exception exception)
                when (exception is ArgumentException or
                    InvalidOperationException or
                    KeyNotFoundException)
            {
                return JsonSerializer.Serialize(
                    new
                    {
                        workspaceId,
                        success = false,
                        error = exception.Message
                    },
                    WorkspaceToolJson.Options);
            }
        }
    }
}
