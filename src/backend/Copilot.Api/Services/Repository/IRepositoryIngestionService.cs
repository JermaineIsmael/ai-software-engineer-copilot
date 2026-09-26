using Copilot.Api.Models.Repository;

namespace Copilot.Api.Services.Repository;

public interface IRepositoryIngestionService
{
    Task<RepositorySnapshot> IngestAsync(
        string repositoryUrl,
        string branch,
        CancellationToken cancellationToken = default);
}
