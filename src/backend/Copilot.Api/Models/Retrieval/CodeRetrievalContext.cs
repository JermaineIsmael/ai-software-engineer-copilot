namespace Copilot.Api.Models.Retrieval;

public sealed record CodeRetrievalContext(
    string Text,
    IReadOnlyList<RetrievedCodeChunk> Chunks);
