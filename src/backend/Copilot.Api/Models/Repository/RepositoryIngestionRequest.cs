namespace Copilot.Api.Models.Repository;

public sealed record RepositoryIngestionRequest(
    string RepositoryUrl,
    string Branch = "main");
