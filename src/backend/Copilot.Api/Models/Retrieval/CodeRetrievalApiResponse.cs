namespace Copilot.Api.Models.Retrieval;

public sealed record CodeRetrievalApiResponse(
    string Query,
    IReadOnlyList<RetrievedCodeChunk> Results,
    CodeRetrievalContext Context);
