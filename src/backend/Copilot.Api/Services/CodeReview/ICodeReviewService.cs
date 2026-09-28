using Copilot.Api.Models.CodeReview;

namespace Copilot.Api.Services.CodeReview;

public interface ICodeReviewService
{
    Task<CodeReviewResponse> ReviewAsync(
        CodeReviewRequest request,
        CancellationToken cancellationToken = default);
}
