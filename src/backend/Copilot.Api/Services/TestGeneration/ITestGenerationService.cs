using Copilot.Api.Models.TestGeneration;

namespace Copilot.Api.Services.TestGeneration;

public interface ITestGenerationService
{
    Task<TestGenerationResponse> GenerateAsync(
        TestGenerationRequest request,
        CancellationToken cancellationToken = default);
}