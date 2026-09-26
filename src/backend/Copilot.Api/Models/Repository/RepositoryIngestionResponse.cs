namespace Copilot.Api.Models.Repository;

public sealed record RepositoryIngestionResponse(
    string Repository,
    string Branch,
    int FileCount,
    int ChunkCount,
    IReadOnlyList<SourceFileMetadata> Files,
    IReadOnlyList<CodeChunk> Chunks);
