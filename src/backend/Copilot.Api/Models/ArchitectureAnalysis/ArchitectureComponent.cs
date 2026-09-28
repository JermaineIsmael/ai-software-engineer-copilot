namespace Copilot.Api.Models.ArchitectureAnalysis;

public sealed record ArchitectureComponent(
    string Name,
    string Responsibility,
    IReadOnlyList<string> Dependencies);