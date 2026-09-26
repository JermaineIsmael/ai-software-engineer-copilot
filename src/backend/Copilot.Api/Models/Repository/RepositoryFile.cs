namespace Copilot.Api.Models.Repository;

public sealed record RepositoryFile(
    string Repository,
    string Branch,
    string Path,
    string Language,
    string Content);
