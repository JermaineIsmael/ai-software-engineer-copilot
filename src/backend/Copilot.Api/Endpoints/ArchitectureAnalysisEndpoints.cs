using Copilot.Api.Models.ArchitectureAnalysis;
using Copilot.Api.Services.ArchitectureAnalysis;

namespace Copilot.Api.Endpoints;

public static class ArchitectureAnalysisEndpoints
{
    public static IEndpointRouteBuilder MapArchitectureAnalysisEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost(
            "/api/architecture-analysis",
            async (
                ArchitectureAnalysisRequest request,
                IArchitectureAnalysisService service,
                CancellationToken cancellationToken) =>
            {
                var response =
                    await service.AnalyzeAsync(
                        request,
                        cancellationToken);

                return Results.Ok(response);
            });

        return endpoints;
    }
}