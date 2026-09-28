namespace Copilot.Api.Models.CodeReview;

public sealed record CodeReviewResponse(
    string Query,
    string Repository,
    string Branch,
    IReadOnlyList<CodeReviewFinding> Findings);
