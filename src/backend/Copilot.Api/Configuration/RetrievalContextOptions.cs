namespace Copilot.Api.Configuration;

public sealed class RetrievalContextOptions
{
    public const string SectionName = "RetrievalContext";

    public int MaxChunks { get; set; } = 8;

    public int MaxCharacters { get; set; } = 12000;
}
