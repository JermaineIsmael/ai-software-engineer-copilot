using Copilot.Api.Models.FileInspection;
using Copilot.Api.Services.FileInspection;

namespace Copilot.Api.Endpoints;

public static class FileInspectionEndpoints
{
    public static IEndpointRouteBuilder MapFileInspectionEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost(
            "/api/file-inspection",
            HandleInspectionAsync);

        return endpoints;
    }

    private static async Task<IResult> HandleInspectionAsync(
        FileInspectionRequest request,
        IFileInspectionService inspectionService,
        ILoggerFactory loggerFactory,
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

        if (string.IsNullOrWhiteSpace(request.RepositoryUrl))
        {
            return Results.BadRequest(
                new
                {
                    error = "Repository URL cannot be empty."
                });
        }

        if (string.IsNullOrWhiteSpace(request.Path))
        {
            return Results.BadRequest(
                new
                {
                    error = "File path cannot be empty."
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

        var logger =
            loggerFactory.CreateLogger(
                "FileInspectionEndpoints");

        try
        {
            var response =
                await inspectionService.InspectAsync(
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
                new
                {
                    error = ex.Message
                });
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "File inspection failed.");

            return Results.Problem(
                statusCode: StatusCodes.Status502BadGateway,
                title: "File inspection failed.",
                detail: "The requested repository file could not be inspected.");
        }
    }
}
