namespace Copilot.Api.Models.Repository;

public sealed record CodeChunkEmbedding(
    string Repository,
    string Branch,
    string Path,
    string Language,
    int ChunkIndex,
    int StartLine,
    int EndLine,
    string Content,
    IReadOnlyList<float> Embedding);