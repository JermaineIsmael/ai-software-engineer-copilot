namespace Copilot.Api.Configuration;

public sealed class BuildTestExecutionOptions
{
    public const string SectionName = "BuildTestExecution";

    public bool Enabled { get; set; } = true;

    public int TimeoutSeconds { get; set; } = 600;

    public int MaximumOutputCharacters { get; set; } = 131_072;
}
