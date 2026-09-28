using Copilot.Api.Models.FileInspection;
using Copilot.Api.Services.Repository;

namespace Copilot.Api.Services.FileInspection;

public interface IFileInspectionService
{
    Task<FileInspectionResponse> InspectAsync(
        FileInspectionRequest request,
        CancellationToken cancellationToken = default);
}
