namespace Copilot.Api.Models.ArchitectureAnalysis;

public sealed record ArchitectureAnalysisResponse(
    string Query,
    string Repository,
    string Branch,
    string Summary,
    IReadOnlyList<ArchitectureComponent> Components,
    IReadOnlyList<string> DataFlows,
    IReadOnlyList<string> IntegrationPoints,
    IReadOnlyList<string> ArchitecturalPatterns,
    IReadOnlyList<ArchitectureRisk> Risks,
    IReadOnlyList<string> Recommendations);