namespace Copilot.Api.Models.Retrieval;

public sealed record CodeRetrievalRequest(
    string Query,
    int Top = 5);
