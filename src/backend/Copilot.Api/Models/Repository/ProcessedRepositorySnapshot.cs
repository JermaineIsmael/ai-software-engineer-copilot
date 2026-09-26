namespace Copilot.Api.Models.Repository;

public sealed record ProcessedRepositorySnapshot(
    string Repository,
    string Branch,
    IReadOnlyList<SourceFileMetadata> Files);
