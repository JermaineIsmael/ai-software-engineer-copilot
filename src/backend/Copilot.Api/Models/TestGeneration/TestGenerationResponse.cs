namespace Copilot.Api.Models.TestGeneration;

public sealed record TestGenerationResponse(
    string Query,
    string Repository,
    string Branch,
    string Framework,
    string TestCode,
    IReadOnlyList<GeneratedTestCase> TestCases);