namespace Copilot.Api.Models.Repository;

public sealed record RepositoryIngestionResponse(
    string Repository,
    string Branch,
    int FileCount,
    IReadOnlyList<string> Files);
