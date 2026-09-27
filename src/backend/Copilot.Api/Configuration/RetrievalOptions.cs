namespace Copilot.Api.Configuration;

public sealed class RetrievalOptions
{
    public const string SectionName = "Retrieval";

    public int DefaultTop { get; set; } = 5;

    public int MaxTop { get; set; } = 20;

    public double MinimumScore { get; set; } = 0.0;
}
