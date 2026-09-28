using Copilot.Api.Models.CodeReview;
using Copilot.Api.Services.CodeReview;

namespace Copilot.Api.Endpoints;

public static class CodeReviewEndpoints
{
    public static IEndpointRouteBuilder MapCodeReviewEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost(
            "/api/code-review",
            async (
                CodeReviewRequest request,
                ICodeReviewService service,
                CancellationToken cancellationToken) =>
            {
                var response =
                    await service.ReviewAsync(
                        request,
                        cancellationToken);

                return Results.Ok(response);
            });

        return endpoints;
    }
}
