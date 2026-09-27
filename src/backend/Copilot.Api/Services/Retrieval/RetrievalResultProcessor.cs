using Copilot.Api.Configuration;
using Copilot.Api.Models.Search;
using Microsoft.Extensions.Options;

namespace Copilot.Api.Services.Retrieval;

public sealed class RetrievalResultProcessor : IRetrievalResultProcessor
{
    private readonly RetrievalOptions _options;

    public RetrievalResultProcessor(
        IOptions<RetrievalOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);

        _options = options.Value;
    }

    public IReadOnlyList<HybridSearchResult> Process(
        IReadOnlyList<HybridSearchResult> results,
        int top)
    {
        ArgumentNullException.ThrowIfNull(results);

        if (top <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(top),
                "Top must be greater than zero.");
        }

        var effectiveTop = Math.Min(top, _options.MaxTop);

        return results
            .Where(result => result.Score >= _options.MinimumScore)
            .OrderByDescending(result => result.Score)
            .ThenBy(result => result.Path, StringComparer.Ordinal)
            .ThenBy(result => result.ChunkIndex)
            .Take(effectiveTop)
            .ToList();
    }
}
