namespace Copilot.Api.Services.Search;

public interface ISearchIndexManagementService
{
    Task EnsureIndexAsync(
        CancellationToken cancellationToken = default);
}
