using System.Text.Json;
using Copilot.Api.Models.Retrieval;
using Copilot.Api.Services.Retrieval;

namespace Copilot.Api.Services.Tools;

public sealed class CodeSearchTool : ICopilotTool
{
    private readonly ICodeRetrievalService _retrievalService;

    public CodeSearchTool(
        ICodeRetrievalService retrievalService)
    {
        ArgumentNullException.ThrowIfNull(retrievalService);

        _retrievalService = retrievalService;
    }

    public string Name => "code_search";

    public string Description =>
        "Search the indexed repository for relevant source code. " +
        "Use this when you need to locate implementations, classes, methods, " +
        "configuration, or other repository code.";

    public string ParametersJsonSchema =>
        """
        {
          "type": "object",
          "properties": {
            "query": {
              "type": "string",
              "description": "The code or technical concept to search for."
            },
            "repository": {
              "type": "string",
              "description": "The repository URL to search."
            },
            "branch": {
              "type": "string",
              "description": "The repository branch.",
              "default": "main"
            },
            "top": {
              "type": "integer",
              "description": "Maximum number of search results.",
              "minimum": 1,
              "maximum": 20,
              "default": 5
            }
          },
          "required": [
            "query",
            "repository"
          ]
        }
        """;

    public async Task<string> ExecuteAsync(
        string argumentsJson,
        CancellationToken cancellationToken = default)
    {
        using var document = JsonDocument.Parse(argumentsJson);

        var root = document.RootElement;

        var query =
            GetRequiredString(root, "query");

        var repository =
            GetRequiredString(root, "repository");

        var branch =
            root.TryGetProperty(
                "branch",
                out var branchElement) &&
            branchElement.ValueKind == JsonValueKind.String
                ? branchElement.GetString() ?? "main"
                : "main";

        var top =
            root.TryGetProperty(
                "top",
                out var topElement) &&
            topElement.TryGetInt32(out var parsedTop)
                ? parsedTop
                : 5;

        if (string.IsNullOrWhiteSpace(branch))
        {
            throw new ArgumentException(
                "The 'branch' argument cannot be empty.");
        }

        if (top <= 0 || top > 20)
        {
            throw new ArgumentException(
                "top must be between 1 and 20.");
        }

        var response =
            await _retrievalService.RetrieveAsync(
                new CodeRetrievalRequest(
                    query,
                    repository,
                    branch,
                    top),
                cancellationToken);

        return JsonSerializer.Serialize(
            new
            {
                response.Query,
                Repository = repository,
                Branch = branch,
                results = response.Results
            });
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
