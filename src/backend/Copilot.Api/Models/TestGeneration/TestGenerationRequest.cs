namespace Copilot.Api.Models.TestGeneration;

public sealed record TestGenerationRequest(
    string Query,
    string Repository,
    string Branch = "main");