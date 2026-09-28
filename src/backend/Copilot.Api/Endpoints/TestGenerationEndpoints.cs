using Copilot.Api.Models.TestGeneration;
using Copilot.Api.Services.TestGeneration;

namespace Copilot.Api.Endpoints;

public static class TestGenerationEndpoints
{
    public static IEndpointRouteBuilder MapTestGenerationEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost(
            "/api/test-generation",
            async (
                TestGenerationRequest request,
                ITestGenerationService service,
                CancellationToken cancellationToken) =>
            {
                var response =
                    await service.GenerateAsync(
                        request,
                        cancellationToken);

                return Results.Ok(response);
            });

        return endpoints;
    }
}