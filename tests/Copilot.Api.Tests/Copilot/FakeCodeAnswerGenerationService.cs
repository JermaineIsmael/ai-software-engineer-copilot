namespace Copilot.Api.Tests.Copilot;

internal sealed class FakeCodeAnswerGenerationService
    : global::Copilot.Api.Services.Copilot.ICodeAnswerGenerationService
{
    public string? LastQuery { get; private set; }

    public string? LastContext { get; private set; }

    public string Answer { get; set; } =
        "The handler is implemented in the retrieved code.";

    public Task<string> GenerateAsync(
        string query,
        string context,
        CancellationToken cancellationToken = default)
    {
        LastQuery = query;
        LastContext = context;

        return Task.FromResult(Answer);
    }
}
