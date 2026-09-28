namespace Copilot.Api.Models.ArchitectureAnalysis;

public sealed record ArchitectureRisk(
    string Severity,
    string Area,
    string Description,
    string Recommendation);