using Copilot.Api.Models.Repository;

namespace Copilot.Api.Services.Repository;

public interface IRepositoryProvider
{
    Task<RepositorySnapshot> GetRepositoryAsync(
        string repositoryUrl,
        string branch,
        CancellationToken cancellationToken = default);

    Task<RepositoryFile> GetFileAsync(
        string repositoryUrl,
        string path,
        string branch,
        CancellationToken cancellationToken = default);
}
