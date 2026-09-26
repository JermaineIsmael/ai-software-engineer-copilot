using Copilot.Api.Models.Repository;

namespace Copilot.Api.Services.Repository;

public sealed class RepositoryIngestionService : IRepositoryIngestionService
{
    private readonly IRepositoryProvider _repositoryProvider;
    private readonly ISourceFileMetadataService _metadataService;
    private readonly ICodeChunkingService _chunkingService;
    private readonly ILogger<RepositoryIngestionService> _logger;

    public RepositoryIngestionService(
        IRepositoryProvider repositoryProvider,
        ISourceFileMetadataService metadataService,
        ICodeChunkingService chunkingService,
        ILogger<RepositoryIngestionService> logger)
    {
        _repositoryProvider = repositoryProvider;
        _metadataService = metadataService;
        _chunkingService = chunkingService;
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

        var chunks = metadataFiles
            .SelectMany(_chunkingService.Chunk)
            .ToList();

        var processedSnapshot = new ProcessedRepositorySnapshot(
            snapshot.Repository,
            snapshot.Branch,
            metadataFiles,
            chunks);

        _logger.LogInformation(
            "Repository ingestion completed for {Repository}. Retrieved {FileCount} files and created {ChunkCount} chunks.",
            processedSnapshot.Repository,
            processedSnapshot.Files.Count,
            processedSnapshot.Chunks.Count);

        return processedSnapshot;
    }
}
