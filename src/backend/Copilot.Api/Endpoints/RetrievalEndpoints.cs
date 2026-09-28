using Copilot.Api.Models.Retrieval;
using Copilot.Api.Services.Retrieval;

namespace Copilot.Api.Endpoints;

public static class RetrievalEndpoints
{
    public static IEndpointRouteBuilder MapRetrievalEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost(
            "/api/retrieval",
            HandleRetrievalAsync);

        return endpoints;
    }

    private static async Task<IResult> HandleRetrievalAsync(
        CodeRetrievalRequest request,
        ICodeRetrievalService retrievalService,
        ICodeContextBuilder contextBuilder,
        CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return Results.BadRequest(
                new
                {
                    error = "Request cannot be null."
                });
        }

        if (string.IsNullOrWhiteSpace(request.Query))
        {
            return Results.BadRequest(
                new
                {
                    error = "Query cannot be empty."
                });
        }

        if (string.IsNullOrWhiteSpace(request.Repository))
        {
            return Results.BadRequest(
                new
                {
                    error = "Repository cannot be empty."
                });
        }

        if (string.IsNullOrWhiteSpace(request.Branch))
        {
            return Results.BadRequest(
                new
                {
                    error = "Branch cannot be empty."
                });
        }

        if (request.Top <= 0)
        {
            return Results.BadRequest(
                new
                {
                    error = "Top must be greater than zero."
                });
        }

        var retrievalResponse =
            await retrievalService.RetrieveAsync(
                request,
                cancellationToken);

        var context =
            contextBuilder.Build(
                retrievalResponse.Results);

        var response = new CodeRetrievalApiResponse(
            retrievalResponse.Query,
            retrievalResponse.Results,
            context);

        return Results.Ok(response);
    }
}
