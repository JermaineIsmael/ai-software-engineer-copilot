using System.Text.Json;
using Copilot.Api.Models.TestGeneration;
using Copilot.Api.Services.Copilot;
using Copilot.Api.Services.Tools;

namespace Copilot.Api.Services.TestGeneration;

public sealed class TestGenerationService : ITestGenerationService
{
    private readonly IToolCallingCodeAnswerGenerationService _answerGenerationService;
    private readonly ICopilotToolRegistry _toolRegistry;

    public TestGenerationService(
        IToolCallingCodeAnswerGenerationService answerGenerationService,
        ICopilotToolRegistry toolRegistry)
    {
        ArgumentNullException.ThrowIfNull(answerGenerationService);
        ArgumentNullException.ThrowIfNull(toolRegistry);

        _answerGenerationService = answerGenerationService;
        _toolRegistry = toolRegistry;
    }

    public async Task<TestGenerationResponse> GenerateAsync(
        TestGenerationRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

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

        var tools = _toolRegistry.GetTools();

        if (tools.Count == 0)
        {
            throw new InvalidOperationException(
                "No Copilot tools are registered.");
        }

        var prompt = BuildPrompt(request);

        var generated =
            await _answerGenerationService.GenerateAsync(
                prompt,
                request.Repository,
                request.Branch,
                tools,
                cancellationToken);

        return ParseResponse(
            generated,
            request);
    }

    private static string BuildPrompt(
        TestGenerationRequest request)
    {
        var prompt = """
        Generate automated tests for the requested repository code.

        User request:
        """ + request.Query + """

        Repository:
        """ + request.Repository + """

        Branch:
        """ + request.Branch + """

        You have access to repository tools.

        Use the available code_search and file_inspection tools to gather
        sufficient repository evidence before generating tests.

        Test generation requirements:
        - Identify the actual implementation under test.
        - Inspect relevant dependencies and existing test patterns when useful.
        - Follow the repository's existing testing conventions when evidence is available.
        - Cover normal behavior.
        - Cover important edge cases.
        - Cover validation and error handling when applicable.
        - Cover important dependency interactions when applicable.
        - Do not invent files, classes, methods, APIs, dependencies, or behavior.
        - Do not invent line numbers.
        - If repository evidence is insufficient, make that limitation clear rather
          than fabricating implementation details.
        - Generate complete test code rather than pseudocode.
        - Prefer focused, maintainable tests over excessive test cases.

        Return ONLY valid JSON using this exact structure:

        {
          "framework": "xUnit",
          "testCode": "complete test source code",
          "testCases": [
            {
              "name": "test method name",
              "scenario": "what behavior is being tested",
              "expectedBehavior": "what the test expects"
            }
          ]
        }

        The JSON must not be wrapped in markdown code fences.
        """;

        return prompt;
    }
    private static TestGenerationResponse ParseResponse(
        string generated,
        TestGenerationRequest request)
    {
        if (string.IsNullOrWhiteSpace(generated))
        {
            throw new InvalidOperationException(
                "The test generation service returned an empty response.");
        }

        try
        {
            using var document =
                JsonDocument.Parse(generated);

            var root = document.RootElement;

            var framework =
                GetRequiredString(root, "framework");

            var testCode =
                GetRequiredString(root, "testCode");

            if (!root.TryGetProperty(
                    "testCases",
                    out var testCasesElement) ||
                testCasesElement.ValueKind != JsonValueKind.Array)
            {
                throw new InvalidOperationException(
                    "The test generation response does not contain a valid testCases array.");
            }

            var testCases = new List<GeneratedTestCase>();

            foreach (var element in testCasesElement.EnumerateArray())
            {
                var name =
                    GetRequiredString(element, "name");

                var scenario =
                    GetRequiredString(element, "scenario");

                var expectedBehavior =
                    GetRequiredString(
                        element,
                        "expectedBehavior");

                testCases.Add(
                    new GeneratedTestCase(
                        name,
                        scenario,
                        expectedBehavior));
            }

            return new TestGenerationResponse(
                request.Query,
                request.Repository,
                request.Branch,
                framework,
                testCode,
                testCases);
        }
        catch (JsonException exception)
        {
            throw new InvalidOperationException(
                "The test generation service returned invalid JSON.",
                exception);
        }
    }

    private static string GetRequiredString(
        JsonElement element,
        string propertyName)
    {
        if (!element.TryGetProperty(
                propertyName,
                out var property) ||
            property.ValueKind != JsonValueKind.String)
        {
            throw new InvalidOperationException(
                $"The test generation response is missing '{propertyName}'.");
        }

        var value = property.GetString();

        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException(
                $"The test generation response contains an empty '{propertyName}'.");
        }

        return value;
    }
}
