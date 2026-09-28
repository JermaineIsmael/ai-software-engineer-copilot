namespace Copilot.Api.Models.ArchitectureAnalysis;

public sealed record ArchitectureAnalysisRequest(
    string Query,
    string Repository,
    string Branch = "main");