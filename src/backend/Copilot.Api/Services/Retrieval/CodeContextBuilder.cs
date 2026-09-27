using System.Text;
using Copilot.Api.Configuration;
using Copilot.Api.Models.Retrieval;
using Microsoft.Extensions.Options;

namespace Copilot.Api.Services.Retrieval;

public sealed class CodeContextBuilder : ICodeContextBuilder
{
    private readonly RetrievalContextOptions _options;

    public CodeContextBuilder(
        IOptions<RetrievalContextOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);

        _options = options.Value;

        if (_options.MaxChunks <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(options),
                "MaxChunks must be greater than zero.");
        }

        if (_options.MaxCharacters <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(options),
                "MaxCharacters must be greater than zero.");
        }
    }

    public CodeRetrievalContext Build(
        IReadOnlyList<RetrievedCodeChunk> chunks)
    {
        ArgumentNullException.ThrowIfNull(chunks);

        var orderedChunks = chunks
            .Where(chunk => !string.IsNullOrWhiteSpace(chunk.Id))
            .GroupBy(chunk => chunk.Id, StringComparer.Ordinal)
            .Select(group => group.First())
            .OrderByDescending(chunk => chunk.Score)
            .ThenBy(chunk => chunk.Path, StringComparer.Ordinal)
            .ThenBy(chunk => chunk.ChunkIndex)
            .Take(_options.MaxChunks)
            .ToList();

        var selectedChunks = new List<RetrievedCodeChunk>();
        var contextBuilder = new StringBuilder();

        foreach (var chunk in orderedChunks)
        {
            var block = BuildChunkBlock(chunk);

            if (contextBuilder.Length + block.Length <= _options.MaxCharacters)
            {
                contextBuilder.Append(block);
                selectedChunks.Add(chunk);
                continue;
            }

            var remainingCharacters =
                _options.MaxCharacters - contextBuilder.Length;

            if (remainingCharacters <= 0)
            {
                break;
            }

            var truncatedBlock = block[..remainingCharacters];

            contextBuilder.Append(truncatedBlock);
            selectedChunks.Add(chunk);

            break;
        }

        return new CodeRetrievalContext(
            contextBuilder.ToString(),
            selectedChunks);
    }

    private static string BuildChunkBlock(
        RetrievedCodeChunk chunk)
    {
        var builder = new StringBuilder();

        builder.AppendLine("--- BEGIN CODE CHUNK ---");
        builder.AppendLine($"Repository: {chunk.Repository}");
        builder.AppendLine($"Branch: {chunk.Branch}");
        builder.AppendLine($"File: {chunk.Path}");
        builder.AppendLine($"Language: {chunk.Language}");
        builder.AppendLine(
            $"Lines: {chunk.StartLine}-{chunk.EndLine}");
        builder.AppendLine($"Score: {chunk.Score:F4}");
        builder.AppendLine("Content:");
        builder.AppendLine(chunk.Content);
        builder.AppendLine("--- END CODE CHUNK ---");
        builder.AppendLine();

        return builder.ToString();
    }
}
