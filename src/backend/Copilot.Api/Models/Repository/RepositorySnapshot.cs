namespace Copilot.Api.Models.Repository;

public sealed record RepositorySnapshot(
    string Repository,
    string Branch,
    IReadOnlyList<RepositoryFile> Files);
