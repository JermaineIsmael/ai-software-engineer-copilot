using Copilot.Api.Models.Repository;

namespace Copilot.Api.Services.Repository;

public interface ICodeChunkingService
{
    IReadOnlyList<CodeChunk> Chunk(SourceFileMetadata file);
}
