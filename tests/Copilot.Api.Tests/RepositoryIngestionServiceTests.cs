using Copilot.Api.Models.Repository;
using Copilot.Api.Services.Repository;
using Microsoft.Extensions.Logging.Abstractions;

namespace Copilot.Api.Tests;

public class RepositoryIngestionServiceTests
{
    [Fact]
    public async Task IngestAsync_ReturnsProcessedSnapshotWithMetadataChunksAndEmbeddings()
    {
        var repositoryProvider = new FakeRepositoryProvider(
            new RepositorySnapshot(
                "owner/repository",
                "main",
                new[]
                {
                    new RepositoryFile(
                        "owner/repository",
                        "main",
                        "src/Test.cs",
                        "csharp",
                        "using System;\nConsole.WriteLine(\"Hello\");")
                }));

        var metadataService = new SourceFileMetadataService(
            new SourceFileClassifier());

        var chunkingService = new CodeChunkingService();
        var embeddingService = new FakeEmbeddingService();

        var service = new RepositoryIngestionService(
            repositoryProvider,
            metadataService,
            chunkingService,
            embeddingService,
            NullLogger<RepositoryIngestionService>.Instance);

        var result = await service.IngestAsync(
            "https://github.com/owner/repository",
            "main");

        Assert.Equal("owner/repository", result.Repository);
        Assert.Equal("main", result.Branch);

        Assert.Single(result.Files);
        Assert.Single(result.Chunks);
        Assert.Single(result.Embeddings);

        Assert.Equal(
            result.Chunks[0].Content,
            result.Embeddings[0].Content);

        Assert.Equal(
            result.Chunks[0].Path,
            result.Embeddings[0].Path);

        Assert.Equal(
            result.Chunks[0].StartLine,
            result.Embeddings[0].StartLine);

        Assert.Equal(
            result.Chunks[0].EndLine,
            result.Embeddings[0].EndLine);

        Assert.Equal(
            new float[] { 0.1f, 0.2f, 0.3f },
            result.Embeddings[0].Embedding);
    }

    [Fact]
    public async Task IngestAsync_PassesChunksToEmbeddingService()
    {
        var repositoryProvider = new FakeRepositoryProvider(
            new RepositorySnapshot(
                "owner/repository",
                "main",
                new[]
                {
                    new RepositoryFile(
                        "owner/repository",
                        "main",
                        "src/Test.cs",
                        "csharp",
                        "line 1\nline 2\nline 3")
                }));

        var metadataService = new SourceFileMetadataService(
            new SourceFileClassifier());

        var chunkingService = new CodeChunkingService();
        var embeddingService = new FakeEmbeddingService();

        var service = new RepositoryIngestionService(
            repositoryProvider,
            metadataService,
            chunkingService,
            embeddingService,
            NullLogger<RepositoryIngestionService>.Instance);

        await service.IngestAsync(
            "https://github.com/owner/repository",
            "main");

        Assert.NotNull(embeddingService.ReceivedChunks);
        Assert.Single(embeddingService.ReceivedChunks!);

        Assert.Equal(
            "src/Test.cs",
            embeddingService.ReceivedChunks![0].Path);
    }

    [Fact]
    public async Task IngestAsync_PassesCancellationTokenToEmbeddingService()
    {
        var repositoryProvider = new FakeRepositoryProvider(
            new RepositorySnapshot(
                "owner/repository",
                "main",
                new[]
                {
                    new RepositoryFile(
                        "owner/repository",
                        "main",
                        "src/Test.cs",
                        "csharp",
                        "Console.WriteLine(\"Hello\");")
                }));

        var metadataService = new SourceFileMetadataService(
            new SourceFileClassifier());

        var chunkingService = new CodeChunkingService();
        var embeddingService = new FakeEmbeddingService();

        var service = new RepositoryIngestionService(
            repositoryProvider,
            metadataService,
            chunkingService,
            embeddingService,
            NullLogger<RepositoryIngestionService>.Instance);

        using var cancellationTokenSource =
            new CancellationTokenSource();

        await service.IngestAsync(
            "https://github.com/owner/repository",
            "main",
            cancellationTokenSource.Token);

        Assert.Equal(
            cancellationTokenSource.Token,
            embeddingService.ReceivedCancellationToken);
    }

    [Fact]
    public async Task IngestAsync_ThrowsWhenRepositoryUrlIsMissing()
    {
        var service = CreateService();

        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => service.IngestAsync(
                string.Empty,
                "main"));

        Assert.Equal(
            "repositoryUrl",
            exception.ParamName);
    }

    [Fact]
    public async Task IngestAsync_ThrowsWhenBranchIsMissing()
    {
        var service = CreateService();

        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => service.IngestAsync(
                "https://github.com/owner/repository",
                string.Empty));

        Assert.Equal(
            "branch",
            exception.ParamName);
    }

    private static RepositoryIngestionService CreateService()
    {
        var repositoryProvider = new FakeRepositoryProvider(
            new RepositorySnapshot(
                "owner/repository",
                "main",
                Array.Empty<RepositoryFile>()));

        var metadataService = new SourceFileMetadataService(
            new SourceFileClassifier());

        var chunkingService = new CodeChunkingService();
        var embeddingService = new FakeEmbeddingService();

        return new RepositoryIngestionService(
            repositoryProvider,
            metadataService,
            chunkingService,
            embeddingService,
            NullLogger<RepositoryIngestionService>.Instance);
    }

    private sealed class FakeRepositoryProvider : IRepositoryProvider
    {
        private readonly RepositorySnapshot _snapshot;

        public FakeRepositoryProvider(
            RepositorySnapshot snapshot)
        {
            _snapshot = snapshot;
        }

        public Task<RepositoryFile> GetFileAsync(
            string repositoryUrl,
            string path,
            string branch,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                new RepositoryFile(
                    "test/repository",
                    branch,
                    path,
                    "cs",
                    "test content"));
        }
        public Task<RepositorySnapshot> GetRepositoryAsync(
            string repositoryUrl,
            string branch,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_snapshot);
        }
    }

    private sealed class FakeEmbeddingService : IEmbeddingService
    {
        public IReadOnlyList<CodeChunk>? ReceivedChunks { get; private set; }

        public CancellationToken ReceivedCancellationToken { get; private set; }

        public Task<IReadOnlyList<CodeChunkEmbedding>> GenerateEmbeddingsAsync(
            IReadOnlyList<CodeChunk> chunks,
            CancellationToken cancellationToken = default)
        {
            ReceivedChunks = chunks;
            ReceivedCancellationToken = cancellationToken;

            var embeddings = chunks
                .Select(chunk =>
                    new CodeChunkEmbedding(
                        chunk.Repository,
                        chunk.Branch,
                        chunk.Path,
                        chunk.Language,
                        chunk.ChunkIndex,
                        chunk.StartLine,
                        chunk.EndLine,
                        chunk.Content,
                        new[] { 0.1f, 0.2f, 0.3f }))
                .ToList();

            return Task.FromResult<
                IReadOnlyList<CodeChunkEmbedding>>(embeddings);
        }
    }
}
