namespace Copilot.Api.Models.Search;

public sealed record HybridSearchResult(
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
