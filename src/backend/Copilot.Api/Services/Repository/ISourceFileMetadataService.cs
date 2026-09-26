using Copilot.Api.Models.Repository;

namespace Copilot.Api.Services.Repository;

public interface ISourceFileMetadataService
{
    SourceFileMetadata Extract(RepositoryFile file);
}
