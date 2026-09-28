using System.Text.Json;
using Copilot.Api.Models.ArchitectureAnalysis;
using Copilot.Api.Services.Copilot;
using Copilot.Api.Services.Tools;

namespace Copilot.Api.Services.ArchitectureAnalysis;

public sealed class ArchitectureAnalysisService : IArchitectureAnalysisService
{
    private readonly IToolCallingCodeAnswerGenerationService _answerGenerationService;
    private readonly ICopilotToolRegistry _toolRegistry;

    public ArchitectureAnalysisService(
        IToolCallingCodeAnswerGenerationService answerGenerationService,
        ICopilotToolRegistry toolRegistry)
    {
        ArgumentNullException.ThrowIfNull(answerGenerationService);
        ArgumentNullException.ThrowIfNull(toolRegistry);

        _answerGenerationService = answerGenerationService;
        _toolRegistry = toolRegistry;
    }

    public async Task<ArchitectureAnalysisResponse> AnalyzeAsync(
        ArchitectureAnalysisRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        Validate(request);

        var tools = _toolRegistry.GetTools();

        if (tools.Count == 0)
        {
            throw new InvalidOperationException(
                "At least one Copilot tool must be registered.");
        }

        var prompt = BuildPrompt(request);

        var response =
            await _answerGenerationService.GenerateAsync(
                prompt,
                request.Repository,
                request.Branch,
                tools,
                cancellationToken);

        return ParseResponse(response, request);
    }

    private static void Validate(
        ArchitectureAnalysisRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Query))
        {
            throw new ArgumentException(
                "The query cannot be empty.",
                nameof(request));
        }

        if (string.IsNullOrWhiteSpace(request.Repository))
        {
            throw new ArgumentException(
                "The repository cannot be empty.",
                nameof(request));
        }

        if (string.IsNullOrWhiteSpace(request.Branch))
        {
            throw new ArgumentException(
                "The branch cannot be empty.",
                nameof(request));
        }
    }

    private static string BuildPrompt(
        ArchitectureAnalysisRequest request)
    {
        var prompt = """
        Analyze the architecture of the requested software repository.

        User request:
        """ + request.Query + """

        Repository:
        """ + request.Repository + """

        Branch:
        """ + request.Branch + """

        You have access to repository tools.

        Use code_search and file_inspection to gather sufficient repository
        evidence before producing the architecture analysis.

        Architecture analysis requirements:

        - Identify the major architectural components.
        - Explain the responsibility of each major component.
        - Identify important dependencies between components.
        - Describe important data flows when repository evidence supports them.
        - Identify external integration points such as APIs, databases,
          messaging systems, cloud services, or other external dependencies.
        - Identify architectural patterns only when supported by repository evidence.
        - Identify important architectural risks, coupling, bottlenecks,
          reliability concerns, security concerns, scalability concerns,
          or maintainability concerns when evidence supports them.
        - Provide practical recommendations.
        - Do not invent files, classes, methods, services, dependencies,
          integrations, architectural patterns, or behavior.
        - Do not invent line numbers.
        - If evidence is insufficient, clearly indicate the limitation.
        - Distinguish observed architecture from recommendations.

        Return ONLY valid JSON using this structure:

        {
          "summary": "overall architecture summary",
          "components": [
            {
              "name": "component name",
              "responsibility": "component responsibility",
              "dependencies": [
                "dependency name"
              ]
            }
          ],
          "dataFlows": [
            "description of an observed data flow"
          ],
          "integrationPoints": [
            "description of an observed integration"
          ],
          "architecturalPatterns": [
            "observed architectural pattern"
          ],
          "risks": [
            {
              "severity": "Critical|High|Medium|Low|Informational",
              "area": "architecture area",
              "description": "risk description",
              "recommendation": "recommended improvement"
            }
          ],
          "recommendations": [
            "practical architecture recommendation"
          ]
        }

        The JSON must not be wrapped in markdown code fences.
        """;

        return prompt;
    }

    private static ArchitectureAnalysisResponse ParseResponse(
        string response,
        ArchitectureAnalysisRequest request)
    {
        if (string.IsNullOrWhiteSpace(response))
        {
            throw new InvalidOperationException(
                "The architecture analysis response was empty.");
        }

        try
        {
            using var document =
                JsonDocument.Parse(response);

            var root = document.RootElement;

            if (!root.TryGetProperty(
                    "summary",
                    out var summaryElement) ||
                summaryElement.ValueKind != JsonValueKind.String)
            {
                throw new InvalidOperationException(
                    "The architecture analysis response is missing 'summary'.");
            }

            var summary = summaryElement.GetString();

            if (string.IsNullOrWhiteSpace(summary))
            {
                throw new InvalidOperationException(
                    "The architecture analysis summary cannot be empty.");
            }

            var components = ParseComponents(root);
            var dataFlows = ParseStringArray(root, "dataFlows");
            var integrationPoints = ParseStringArray(root, "integrationPoints");
            var architecturalPatterns = ParseStringArray(root, "architecturalPatterns");
            var risks = ParseRisks(root);
            var recommendations = ParseStringArray(root, "recommendations");

            return new ArchitectureAnalysisResponse(
                request.Query,
                request.Repository,
                request.Branch,
                summary,
                components,
                dataFlows,
                integrationPoints,
                architecturalPatterns,
                risks,
                recommendations);
        }
        catch (JsonException exception)
        {
            throw new InvalidOperationException(
                "The architecture analysis response was not valid JSON.",
                exception);
        }
    }

    private static IReadOnlyList<ArchitectureComponent> ParseComponents(
        JsonElement root)
    {
        if (!root.TryGetProperty(
                "components",
                out var element) ||
            element.ValueKind != JsonValueKind.Array)
        {
            throw new InvalidOperationException(
                "The architecture analysis response is missing 'components'.");
        }

        var components = new List<ArchitectureComponent>();

        foreach (var component in element.EnumerateArray())
        {
            if (!component.TryGetProperty("name", out var nameElement) ||
                !component.TryGetProperty("responsibility", out var responsibilityElement) ||
                !component.TryGetProperty("dependencies", out var dependenciesElement))
            {
                throw new InvalidOperationException(
                    "An architecture component is missing required properties.");
            }

            var name = nameElement.GetString();
            var responsibility = responsibilityElement.GetString();

            if (string.IsNullOrWhiteSpace(name) ||
                string.IsNullOrWhiteSpace(responsibility) ||
                dependenciesElement.ValueKind != JsonValueKind.Array)
            {
                throw new InvalidOperationException(
                    "An architecture component contains invalid values.");
            }

            var dependencies =
                dependenciesElement
                    .EnumerateArray()
                    .Where(item => item.ValueKind == JsonValueKind.String)
                    .Select(item => item.GetString() ?? string.Empty)
                    .Where(value => !string.IsNullOrWhiteSpace(value))
                    .ToArray();

            components.Add(
                new ArchitectureComponent(
                    name,
                    responsibility,
                    dependencies));
        }

        return components;
    }

    private static IReadOnlyList<ArchitectureRisk> ParseRisks(
        JsonElement root)
    {
        if (!root.TryGetProperty("risks", out var element) ||
            element.ValueKind != JsonValueKind.Array)
        {
            throw new InvalidOperationException(
                "The architecture analysis response is missing 'risks'.");
        }

        var risks = new List<ArchitectureRisk>();

        foreach (var risk in element.EnumerateArray())
        {
            if (!risk.TryGetProperty("severity", out var severityElement) ||
                !risk.TryGetProperty("area", out var areaElement) ||
                !risk.TryGetProperty("description", out var descriptionElement) ||
                !risk.TryGetProperty("recommendation", out var recommendationElement))
            {
                throw new InvalidOperationException(
                    "An architecture risk is missing required properties.");
            }

            var severity = severityElement.GetString();
            var area = areaElement.GetString();
            var description = descriptionElement.GetString();
            var recommendation = recommendationElement.GetString();

            if (string.IsNullOrWhiteSpace(severity) ||
                string.IsNullOrWhiteSpace(area) ||
                string.IsNullOrWhiteSpace(description) ||
                string.IsNullOrWhiteSpace(recommendation))
            {
                throw new InvalidOperationException(
                    "An architecture risk contains an empty required value.");
            }

            risks.Add(
                new ArchitectureRisk(
                    severity,
                    area,
                    description,
                    recommendation));
        }

        return risks;
    }

    private static IReadOnlyList<string> ParseStringArray(
        JsonElement root,
        string propertyName)
    {
        if (!root.TryGetProperty(propertyName, out var element) ||
            element.ValueKind != JsonValueKind.Array)
        {
            throw new InvalidOperationException(
                $"The architecture analysis response is missing '{propertyName}'.");
        }

        return element
            .EnumerateArray()
            .Where(item => item.ValueKind == JsonValueKind.String)
            .Select(item => item.GetString() ?? string.Empty)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .ToArray();
    }
}