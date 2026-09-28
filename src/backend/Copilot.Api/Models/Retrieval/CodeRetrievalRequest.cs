namespace Copilot.Api.Models.Retrieval;

public sealed record CodeRetrievalRequest(
    string Query,
    string Repository,
    string Branch = "main",
    int Top = 5);
