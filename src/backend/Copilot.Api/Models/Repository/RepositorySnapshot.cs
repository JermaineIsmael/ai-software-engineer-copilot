namespace Copilot.Api.Models.Repository;

public sealed record RepositorySnapshot(
    string Repository,
    string Branch,
    IReadOnlyList<RepositoryFile> Files)
{
    public string? CommitSha { get; init; }

    public string? DefaultBranch { get; init; }

    public IReadOnlyList<string> FailedFiles { get; init; } = [];
}
