namespace Copilot.Api.Models.CodeReview;

public sealed record CodeReviewRequest(
    string Query,
    string Repository,
    string Branch = "main");
