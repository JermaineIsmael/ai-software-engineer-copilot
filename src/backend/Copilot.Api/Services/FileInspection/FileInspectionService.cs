using Copilot.Api.Models.FileInspection;
using Copilot.Api.Services.Repository;

namespace Copilot.Api.Services.FileInspection;

public sealed class FileInspectionService : IFileInspectionService
{
    private readonly IRepositoryProvider _repositoryProvider;

    public FileInspectionService(
        IRepositoryProvider repositoryProvider)
    {
        ArgumentNullException.ThrowIfNull(repositoryProvider);

        _repositoryProvider = repositoryProvider;
    }

    public async Task<FileInspectionResponse> InspectAsync(
        FileInspectionRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(request.RepositoryUrl))
        {
            throw new ArgumentException(
                "Repository URL cannot be empty.",
                nameof(request));
        }

        if (string.IsNullOrWhiteSpace(request.Path))
        {
            throw new ArgumentException(
                "File path cannot be empty.",
                nameof(request));
        }

        if (string.IsNullOrWhiteSpace(request.Branch))
        {
            throw new ArgumentException(
                "Branch cannot be empty.",
                nameof(request));
        }

        cancellationToken.ThrowIfCancellationRequested();

        var file = await _repositoryProvider.GetFileAsync(
            request.RepositoryUrl,
            request.Path,
            request.Branch,
            cancellationToken);

        return new FileInspectionResponse(
            file.Repository,
            file.Branch,
            file.Path,
            file.Language,
            file.Content);
    }
}
