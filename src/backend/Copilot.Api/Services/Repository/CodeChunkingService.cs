using Copilot.Api.Models.Repository;

namespace Copilot.Api.Services.Repository;

public sealed class CodeChunkingService : ICodeChunkingService
{
    private const int DefaultMaxLines = 40;
    private const int DefaultOverlapLines = 5;

    private readonly int _maxLines;
    private readonly int _overlapLines;

    public CodeChunkingService(
        int maxLines = DefaultMaxLines,
        int overlapLines = DefaultOverlapLines)
    {
        if (maxLines <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(maxLines),
                "Maximum chunk lines must be greater than zero.");
        }

        if (overlapLines < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(overlapLines),
                "Overlap lines cannot be negative.");
        }

        if (overlapLines >= maxLines)
        {
            throw new ArgumentException(
                "Overlap lines must be less than the maximum chunk lines.",
                nameof(overlapLines));
        }

        _maxLines = maxLines;
        _overlapLines = overlapLines;
    }

    public IReadOnlyList<CodeChunk> Chunk(SourceFileMetadata file)
    {
        ArgumentNullException.ThrowIfNull(file);

        if (string.IsNullOrEmpty(file.Content))
        {
            return Array.Empty<CodeChunk>();
        }

        var newline = DetectNewline(file.Content);

        var lines = file.Content
            .Split(
                new[] { "\r\n", "\n", "\r" },
                StringSplitOptions.None);

        var chunks = new List<CodeChunk>();

        var startIndex = 0;
        var chunkIndex = 0;

        while (startIndex < lines.Length)
        {
            var endIndex = Math.Min(
                startIndex + _maxLines,
                lines.Length);

            var chunkLines = lines[startIndex..endIndex];

            var content = string.Join(
                newline,
                chunkLines);

            chunks.Add(
                new CodeChunk(
                    file.Repository,
                    file.Branch,
                    file.Path,
                    file.Language,
                    chunkIndex,
                    startIndex + 1,
                    endIndex,
                    content));

            if (endIndex >= lines.Length)
            {
                break;
            }

            startIndex = endIndex - _overlapLines;
            chunkIndex++;
        }

        return chunks;
    }

    private static string DetectNewline(string content)
    {
        var crlfIndex = content.IndexOf(
            "\r\n",
            StringComparison.Ordinal);

        if (crlfIndex >= 0)
        {
            return "\r\n";
        }

        if (content.Contains('\n'))
        {
            return "\n";
        }

        if (content.Contains('\r'))
        {
            return "\r";
        }

        return string.Empty;
    }
}
