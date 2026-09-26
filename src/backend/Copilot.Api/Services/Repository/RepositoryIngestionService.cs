using Copilot.Api.Models.Repository;

namespace Copilot.Api.Services.Repository;

public sealed class RepositoryIngestionService : IRepositoryIngestionService
{
    private readonly IRepositoryProvider _repositoryProvider;
    private readonly ILogger<RepositoryIngestionService> _logger;

    public RepositoryIngestionService(
        IRepositoryProvider repositoryProvider,
        ILogger<RepositoryIngestionService> logger)
    {
        _repositoryProvider = repositoryProvider;
        _logger = logger;
    }

    public async Task<RepositorySnapshot> IngestAsync(
        string repositoryUrl,
        string branch,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(repositoryUrl))
        {
            throw new ArgumentException(
                "Repository URL is required.",
                nameof(repositoryUrl));
        }

        if (string.IsNullOrWhiteSpace(branch))
        {
            throw new ArgumentException(
                "Branch is required.",
                nameof(branch));
        }

        _logger.LogInformation(
            "Starting repository ingestion for {RepositoryUrl} on branch {Branch}.",
            repositoryUrl,
            branch);

        var snapshot = await _repositoryProvider.GetRepositoryAsync(
            repositoryUrl,
            branch,
            cancellationToken);

        _logger.LogInformation(
            "Repository ingestion completed for {Repository}. Retrieved {FileCount} files.",
            snapshot.Repository,
            snapshot.Files.Count);

        return snapshot;
    }
}
