namespace Copilot.Api.Services.Copilot;

public interface ICodeAnswerGenerationService
{
    Task<string> GenerateAsync(
        string query,
        string context,
        CancellationToken cancellationToken = default);
}
