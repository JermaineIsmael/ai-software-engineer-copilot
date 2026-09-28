using Copilot.Api.Models.ArchitectureAnalysis;

namespace Copilot.Api.Services.ArchitectureAnalysis;

public interface IArchitectureAnalysisService
{
    Task<ArchitectureAnalysisResponse> AnalyzeAsync(
        ArchitectureAnalysisRequest request,
        CancellationToken cancellationToken = default);
}