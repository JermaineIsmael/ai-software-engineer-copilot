using Copilot.Api.Models.Retrieval;
using Copilot.Api.Models.Search;
using Copilot.Api.Services.Search;

namespace Copilot.Api.Services.Retrieval;

public sealed class CodeRetrievalService : ICodeRetrievalService
{
    private readonly IQueryEmbeddingService _queryEmbeddingService;
    private readonly IHybridSearchService _hybridSearchService;
    private readonly IRetrievalResultProcessor _resultProcessor;

    public CodeRetrievalService(
        IQueryEmbeddingService queryEmbeddingService,
        IHybridSearchService hybridSearchService,
        IRetrievalResultProcessor resultProcessor)
    {
        ArgumentNullException.ThrowIfNull(queryEmbeddingService);
        ArgumentNullException.ThrowIfNull(hybridSearchService);
        ArgumentNullException.ThrowIfNull(resultProcessor);

        _queryEmbeddingService = queryEmbeddingService;
        _hybridSearchService = hybridSearchService;
        _resultProcessor = resultProcessor;
    }

    public async Task<CodeRetrievalResponse> RetrieveAsync(
        CodeRetrievalRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(request.Query))
        {
            throw new ArgumentException(
                "Query cannot be empty.",
                nameof(request));
        }

        if (request.Top <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(request.Top),
                "Top must be greater than zero.");
        }

        cancellationToken.ThrowIfCancellationRequested();

        var queryEmbedding =
            await _queryEmbeddingService.GenerateAsync(
                request.Query,
                cancellationToken);

        var searchResults =
            await _hybridSearchService.SearchAsync(
                request.Query,
                queryEmbedding,
                request.Top,
                cancellationToken);

        var processedResults =
            _resultProcessor.Process(
                searchResults,
                request.Top);

        var results = processedResults
            .Select(MapResult)
            .ToList();

        return new CodeRetrievalResponse(
            request.Query,
            results);
    }

    private static RetrievedCodeChunk MapResult(
        HybridSearchResult result)
    {
        return new RetrievedCodeChunk(
            result.Id,
            result.Repository,
            result.Branch,
            result.Path,
            result.Language,
            result.ChunkIndex,
            result.StartLine,
            result.EndLine,
            result.Content,
            result.Score);
    }
}
