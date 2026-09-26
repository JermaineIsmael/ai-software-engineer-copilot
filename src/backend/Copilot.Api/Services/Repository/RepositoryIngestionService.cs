using Copilot.Api.Models.Repository;

namespace Copilot.Api.Services.Repository;

public sealed class RepositoryIngestionService : IRepositoryIngestionService
{
    private readonly IRepositoryProvider _repositoryProvider;
    private readonly ISourceFileMetadataService _metadataService;
    private readonly ICodeChunkingService _chunkingService;
    private readonly IEmbeddingService _embeddingService;
    private readonly ILogger<RepositoryIngestionService> _logger;

    public RepositoryIngestionService(
        IRepositoryProvider repositoryProvider,
        ISourceFileMetadataService metadataService,
        ICodeChunkingService chunkingService,
        IEmbeddingService embeddingService,
        ILogger<RepositoryIngestionService> logger)
    {
        _repositoryProvider = repositoryProvider;
        _metadataService = metadataService;
        _chunkingService = chunkingService;
        _embeddingService = embeddingService;
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

        _logger.LogInformation(
            "Generating embeddings for {ChunkCount} code chunks.",
            chunks.Count);

        var embeddings = await _embeddingService.GenerateEmbeddingsAsync(
            chunks,
            cancellationToken);

        var processedSnapshot = new ProcessedRepositorySnapshot(
            snapshot.Repository,
            snapshot.Branch,
            metadataFiles,
            chunks,
            embeddings);

        _logger.LogInformation(
            "Repository ingestion completed for {Repository}. Retrieved {FileCount} files, created {ChunkCount} chunks, and generated {EmbeddingCount} embeddings.",
            processedSnapshot.Repository,
            processedSnapshot.Files.Count,
            processedSnapshot.Chunks.Count,
            processedSnapshot.Embeddings.Count);

        return processedSnapshot;
    }
}
