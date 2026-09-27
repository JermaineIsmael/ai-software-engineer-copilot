namespace Copilot.Api.Models.Retrieval;

public sealed record CodeRetrievalResponse(
    string Query,
    IReadOnlyList<RetrievedCodeChunk> Results);
