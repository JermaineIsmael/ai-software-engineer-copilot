namespace Copilot.Api.Models.Retrieval;

public sealed record RetrievedCodeChunk(
    string Id,
    string Repository,
    string Branch,
    string Path,
    string Language,
    int ChunkIndex,
    int StartLine,
    int EndLine,
    string Content,
    double Score);
