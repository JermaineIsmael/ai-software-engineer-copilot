namespace Copilot.Api.Models.CodeReview;

public sealed record CodeReviewFinding(
    string Severity,
    string File,
    string Location,
    string Finding,
    string Explanation,
    string Recommendation);
