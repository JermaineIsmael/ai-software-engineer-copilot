using System.Text.Json;
using Copilot.Api.Models.FileInspection;
using Copilot.Api.Services.FileInspection;

namespace Copilot.Api.Services.Tools;

public sealed class FileInspectionTool : ICopilotTool
{
    private readonly IFileInspectionService _fileInspectionService;

    public FileInspectionTool(
        IFileInspectionService fileInspectionService)
    {
        ArgumentNullException.ThrowIfNull(fileInspectionService);

        _fileInspectionService = fileInspectionService;
    }

    public string Name => "file_inspection";

    public string Description =>
        "Inspect the complete contents of a specific source file in the repository. " +
        "Use this when you need exact file contents, implementation details, " +
        "or code that was not sufficiently returned by code search.";

    public string ParametersJsonSchema =>
        """
        {
          "type": "object",
          "properties": {
            "repositoryUrl": {
              "type": "string",
              "description": "The repository URL."
            },
            "path": {
              "type": "string",
              "description": "The path of the file inside the repository."
            },
            "branch": {
              "type": "string",
              "description": "The repository branch.",
              "default": "main"
            }
          },
          "required": [
            "repositoryUrl",
            "path"
          ]
        }
        """;

    public async Task<string> ExecuteAsync(
        string argumentsJson,
        CancellationToken cancellationToken = default)
    {
        using var document = JsonDocument.Parse(argumentsJson);

        var root = document.RootElement;

        var repositoryUrl =
            GetRequiredString(root, "repositoryUrl");

        var path =
            GetRequiredString(root, "path");

        var branch =
            root.TryGetProperty("branch", out var branchElement) &&
            branchElement.ValueKind == JsonValueKind.String
                ? branchElement.GetString() ?? "main"
                : "main";

        var response =
            await _fileInspectionService.InspectAsync(
                new FileInspectionRequest(
                    repositoryUrl,
                    path,
                    branch),
                cancellationToken);

        return JsonSerializer.Serialize(response);
    }

    private static string GetRequiredString(
        JsonElement root,
        string propertyName)
    {
        if (!root.TryGetProperty(
                propertyName,
                out var element) ||
            element.ValueKind != JsonValueKind.String)
        {
            throw new ArgumentException(
                $"The '{propertyName}' argument is required.");
        }

        var value = element.GetString();

        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException(
                $"The '{propertyName}' argument cannot be empty.");
        }

        return value;
    }
}
