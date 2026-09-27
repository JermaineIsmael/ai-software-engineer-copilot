using Copilot.Api.Models.Copilot;
using Copilot.Api.Models.Retrieval;
using Copilot.Api.Services.Retrieval;

namespace Copilot.Api.Services.Copilot;

public sealed class CodeCopilotService : ICodeCopilotService
{
    private readonly ICodeRetrievalService _retrievalService;
    private readonly ICodeContextBuilder _contextBuilder;
    private readonly ICodeAnswerGenerationService _answerGenerationService;

    public CodeCopilotService(
        ICodeRetrievalService retrievalService,
        ICodeContextBuilder contextBuilder,
        ICodeAnswerGenerationService answerGenerationService)
    {
        ArgumentNullException.ThrowIfNull(retrievalService);
        ArgumentNullException.ThrowIfNull(contextBuilder);
        ArgumentNullException.ThrowIfNull(answerGenerationService);

        _retrievalService = retrievalService;
        _contextBuilder = contextBuilder;
        _answerGenerationService = answerGenerationService;
    }

    public async Task<CopilotResponse> AskAsync(
        CopilotRequest request,
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

        var retrievalResponse =
            await _retrievalService.RetrieveAsync(
                new CodeRetrievalRequest(
                    request.Query,
                    request.Top),
                cancellationToken);

        if (retrievalResponse.Results.Count == 0)
        {
            return new CopilotResponse(
                request.Query,
                "I could not find relevant code in the indexed repository.",
                Array.Empty<CopilotSource>());
        }

        var context =
            _contextBuilder.Build(
                retrievalResponse.Results);

        if (context.Chunks.Count == 0 ||
            string.IsNullOrWhiteSpace(context.Text))
        {
            return new CopilotResponse(
                request.Query,
                "I could not build sufficient repository context to answer the question.",
                Array.Empty<CopilotSource>());
        }

        var answer =
            await _answerGenerationService.GenerateAsync(
                request.Query,
                context.Text,
                cancellationToken);

        var sources = context.Chunks
            .Select(chunk =>
                new CopilotSource(
                    chunk.Repository,
                    chunk.Branch,
                    chunk.Path,
                    chunk.StartLine,
                    chunk.EndLine,
                    chunk.Score))
            .ToList();

        return new CopilotResponse(
            request.Query,
            answer,
            sources);
    }
}
