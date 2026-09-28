namespace Copilot.Api.Models.TestGeneration;

public sealed record GeneratedTestCase(
    string Name,
    string Scenario,
    string ExpectedBehavior);