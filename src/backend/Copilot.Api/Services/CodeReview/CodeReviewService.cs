using System.Text.Json;
using Copilot.Api.Models.CodeReview;
using Copilot.Api.Services.Copilot;
using Copilot.Api.Services.Tools;

namespace Copilot.Api.Services.CodeReview;

public sealed class CodeReviewService : ICodeReviewService
{
    private readonly IToolCallingCodeAnswerGenerationService
        _answerGenerationService;

    private readonly ICopilotToolRegistry
        _toolRegistry;

    public CodeReviewService(
        IToolCallingCodeAnswerGenerationService answerGenerationService,
        ICopilotToolRegistry toolRegistry)
    {
        ArgumentNullException.ThrowIfNull(answerGenerationService);
        ArgumentNullException.ThrowIfNull(toolRegistry);

        _answerGenerationService = answerGenerationService;
        _toolRegistry = toolRegistry;
    }

    public async Task<CodeReviewResponse> ReviewAsync(
        CodeReviewRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        ValidateRequest(request);

        var tools = _toolRegistry.GetTools();

        var reviewPrompt = BuildReviewPrompt(request);

        var generatedResponse =
            await _answerGenerationService.GenerateAsync(
                reviewPrompt,
                request.Repository,
                request.Branch,
                tools,
                cancellationToken);

        return ParseResponse(
            generatedResponse,
            request);
    }

    private static void ValidateRequest(
        CodeReviewRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Query))
        {
            throw new ArgumentException(
                "The review query cannot be empty.",
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

    private static string BuildReviewPrompt(
        CodeReviewRequest request)
    {
        return
            """
            Perform a repository-aware software code review.

            Review request:
            {0}

            Repository:
            {1}

            Branch:
            {2}

            Use the available repository tools to gather concrete evidence
            before producing the review.

            Review the relevant implementation for:
            - correctness and potential bugs
            - security vulnerabilities
            - error handling
            - maintainability
            - performance
            - reliability
            - test coverage

            Do not invent files, line numbers, implementations, or behavior.
            Findings must be grounded in repository evidence.

            Return ONLY valid JSON using this exact structure:

            {{
              "findings": [
                {{
                  "severity": "Critical|High|Medium|Low|Informational",
                  "file": "path/to/file",
                  "location": "method, class, or line reference when available",
                  "finding": "short description",
                  "explanation": "why this matters",
                  "recommendation": "specific recommended improvement"
                }}
              ]
            }}

            If no issues are identified, return:
            {{
              "findings": []
            }}
            """.Replace(
                "{0}", request.Query)
            .Replace(
                "{1}", request.Repository)
            .Replace(
                "{2}", request.Branch);
    }

    private static CodeReviewResponse ParseResponse(
        string generatedResponse,
        CodeReviewRequest request)
    {
        if (string.IsNullOrWhiteSpace(generatedResponse))
        {
            throw new InvalidOperationException(
                "The code review service returned an empty response.");
        }

        try
        {
            using var document =
                JsonDocument.Parse(generatedResponse);

            var root = document.RootElement;

            if (!root.TryGetProperty(
                    "findings",
                    out var findingsElement) ||
                findingsElement.ValueKind != JsonValueKind.Array)
            {
                throw new InvalidOperationException(
                    "The code review response does not contain a valid findings array.");
            }

            var findings = new List<CodeReviewFinding>();

            foreach (var element in findingsElement.EnumerateArray())
            {
                var severity =
                    GetString(element, "severity");

                var file =
                    GetString(element, "file");

                var location =
                    GetString(element, "location");

                var finding =
                    GetString(element, "finding");

                var explanation =
                    GetString(element, "explanation");

                var recommendation =
                    GetString(element, "recommendation");

                findings.Add(
                    new CodeReviewFinding(
                        severity,
                        file,
                        location,
                        finding,
                        explanation,
                        recommendation));
            }

            return new CodeReviewResponse(
                request.Query,
                request.Repository,
                request.Branch,
                findings);
        }
        catch (JsonException exception)
        {
            throw new InvalidOperationException(
                "The code review service returned invalid JSON.",
                exception);
        }
    }

    private static string GetString(
        JsonElement element,
        string propertyName)
    {
        if (!element.TryGetProperty(
                propertyName,
                out var value) ||
            value.ValueKind != JsonValueKind.String)
        {
            throw new InvalidOperationException(
                $"The code review finding is missing '{propertyName}'.");
        }

        return value.GetString() ?? string.Empty;
    }
}
