using Copilot.Api.Models.Copilot;
using Copilot.Api.Services.Copilot;

namespace Copilot.Api.Endpoints;

public static class CopilotEndpoints
{
    public static IEndpointRouteBuilder MapCopilotEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost(
            "/api/copilot/ask",
            HandleAskAsync);

        return endpoints;
    }

    private static async Task<IResult> HandleAskAsync(
        CopilotRequest request,
        ICodeCopilotService copilotService,
        ILoggerFactory loggerFactory,
        CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return Results.BadRequest(
                new CopilotErrorResponse(
                    "Request cannot be null."));
        }

        if (string.IsNullOrWhiteSpace(request.Query))
        {
            return Results.BadRequest(
                new CopilotErrorResponse(
                    "Query cannot be empty."));
        }

        if (string.IsNullOrWhiteSpace(request.Repository))
        {
            return Results.BadRequest(
                new CopilotErrorResponse(
                    "Repository cannot be empty."));
        }

        if (string.IsNullOrWhiteSpace(request.Branch))
        {
            return Results.BadRequest(
                new CopilotErrorResponse(
                    "Branch cannot be empty."));
        }

        if (request.Top <= 0)
        {
            return Results.BadRequest(
                new CopilotErrorResponse(
                    "Top must be greater than zero."));
        }

        var logger =
            loggerFactory.CreateLogger(
                "CopilotEndpoints");

        try
        {
            var response =
                await copilotService.AskAsync(
                    request,
                    cancellationToken);

            return Results.Ok(response);
        }
        catch (OperationCanceledException)
            when (cancellationToken.IsCancellationRequested)
        {
            return Results.StatusCode(
                StatusCodes.Status499ClientClosedRequest);
        }
        catch (ArgumentException ex)
        {
            return Results.BadRequest(
                new CopilotErrorResponse(
                    ex.Message));
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Copilot request failed.");

            return Results.Problem(
                statusCode: StatusCodes.Status502BadGateway,
                title: "Copilot service unavailable.",
                detail: "The Copilot could not complete the request.");
        }
    }
}
