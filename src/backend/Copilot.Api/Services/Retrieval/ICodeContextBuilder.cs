using Copilot.Api.Models.Retrieval;

namespace Copilot.Api.Services.Retrieval;

public interface ICodeContextBuilder
{
    CodeRetrievalContext Build(
        IReadOnlyList<RetrievedCodeChunk> chunks);
}
