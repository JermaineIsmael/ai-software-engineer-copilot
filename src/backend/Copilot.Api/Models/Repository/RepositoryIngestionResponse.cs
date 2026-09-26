namespace Copilot.Api.Models.Repository;

public sealed record RepositoryIngestionResponse(
    string Repository,
    string Branch,
    int FileCount,
    int ChunkCount,
    int EmbeddingCount,
    IReadOnlyList<SourceFileMetadata> Files,
    IReadOnlyList<CodeChunk> Chunks,
    IReadOnlyList<CodeChunkEmbedding> Embeddings);
