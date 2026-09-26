using Copilot.Api.Models.Repository;

namespace Copilot.Api.Services.Repository;

public sealed class RepositoryIngestionService : IRepositoryIngestionService
{
    private readonly IRepositoryProvider _repositoryProvider;
    private readonly ISourceFileMetadataService _metadataService;
    private readonly ILogger<RepositoryIngestionService> _logger;

    public RepositoryIngestionService(
        IRepositoryProvider repositoryProvider,
        ISourceFileMetadataService metadataService,
        ILogger<RepositoryIngestionService> logger)
    {
        _repositoryProvider = repositoryProvider;
        _metadataService = metadataService;
        _logger = logger;
    }

    public async Task<ProcessedRepositorySnapshot> IngestAsync(
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

        var metadataFiles = snapshot.Files
            .Select(_metadataService.Extract)
            .ToList();

        var processedSnapshot = new ProcessedRepositorySnapshot(
            snapshot.Repository,
            snapshot.Branch,
            metadataFiles);

        _logger.LogInformation(
            "Repository ingestion completed for {Repository}. Retrieved {FileCount} files.",
            processedSnapshot.Repository,
            processedSnapshot.Files.Count);

        return processedSnapshot;
    }
}
