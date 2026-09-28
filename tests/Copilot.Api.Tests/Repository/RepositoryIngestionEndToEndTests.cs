using Copilot.Api.Models.Repository;
using Copilot.Api.Services.Repository;
using Microsoft.Extensions.Logging.Abstractions;

namespace Copilot.Api.Tests.Repository;

public sealed class RepositoryIngestionEndToEndTests
{
    [Fact]
    public async Task IngestAsync_ShouldProduceProcessedRepositorySnapshot()
    {
        var repository = new RepositorySnapshot(
            "test/repository",
            "main",
            new[]
            {
                new RepositoryFile(
                    "test/repository",
                    "main",
                    "src/Example.cs",
                    "cs",
                    """
                    namespace Example;

                    public sealed class Example
                    {
                        public string GetValue()
                        {
                            return "hello";
                        }
                    }
                    """)
            });

        var repositoryProvider = new StubRepositoryProvider(repository);

        var classifier = new SourceFileClassifier();
        var metadataService = new SourceFileMetadataService(classifier);

        var chunkingService = new CodeChunkingService(
            maxLines: 40,
            overlapLines: 5);

        var embeddingService = new StubEmbeddingService();

        var service = new RepositoryIngestionService(
            repositoryProvider,
            metadataService,
            chunkingService,
            embeddingService,
            NullLogger<RepositoryIngestionService>.Instance);

        var result = await service.IngestAsync(
            "https://github.com/test/repository",
            "main");

        Assert.NotNull(result);
        Assert.Equal("test/repository", result.Repository);
        Assert.Equal("main", result.Branch);

        Assert.Single(result.Files);
        Assert.NotEmpty(result.Chunks);
        Assert.Equal(result.Chunks.Count, result.Embeddings.Count);

        Assert.Equal("src/Example.cs", result.Files[0].Path);
        Assert.Equal("csharp", result.Files[0].Language);

        Assert.Equal(
            result.Chunks[0].Content,
            result.Embeddings[0].Content);

        Assert.Equal(
            result.Chunks[0].StartLine,
            result.Embeddings[0].StartLine);

        Assert.Equal(
            result.Chunks[0].EndLine,
            result.Embeddings[0].EndLine);
    }

    private sealed class StubRepositoryProvider : IRepositoryProvider
    {
        private readonly RepositorySnapshot _snapshot;

        public StubRepositoryProvider(RepositorySnapshot snapshot)
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

    private sealed class StubEmbeddingService : IEmbeddingService
    {
        public Task<IReadOnlyList<CodeChunkEmbedding>> GenerateEmbeddingsAsync(
            IReadOnlyList<CodeChunk> chunks,
            CancellationToken cancellationToken = default)
        {
            var embeddings = chunks
                .Select(chunk => new CodeChunkEmbedding(
                    chunk.Repository,
                    chunk.Branch,
                    chunk.Path,
                    chunk.Language,
                    chunk.ChunkIndex,
                    chunk.StartLine,
                    chunk.EndLine,
                    chunk.Content,
                    new float[] { 0.1f, 0.2f, 0.3f }))
                .ToList();

            return Task.FromResult<IReadOnlyList<CodeChunkEmbedding>>(
                embeddings);
        }
    }
}
