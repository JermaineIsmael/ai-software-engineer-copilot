using Copilot.Api.Models.Copilot;
using Copilot.Api.Models.Retrieval;
using Copilot.Api.Services.Copilot;
using Copilot.Api.Services.Retrieval;
using Microsoft.Extensions.Options;
using Copilot.Api.Configuration;

namespace Copilot.Api.Tests.Copilot;

public sealed class CodeCopilotServiceTests
{
    [Fact]
    public async Task AskAsync_ReturnsAnswerAndSources()
    {
        var retrievalService = new FakeCodeRetrievalService
        {
            Response = new CodeRetrievalResponse(
                "Where is the handler?",
                new[]
                {
                    new RetrievedCodeChunk(
                        "chunk-1",
                        "repo",
                        "main",
                        "src/Handler.cs",
                        "csharp",
                        0,
                        10,
                        25,
                        "public void Handle() {}",
                        0.95)
                })
        };

        var contextBuilder =
            new CodeContextBuilder(
                Options.Create(
                    new RetrievalContextOptions
                    {
                        MaxChunks = 8,
                        MaxCharacters = 12000
                    }));

        var answerService =
            new FakeCodeAnswerGenerationService
            {
                Answer = "The handler is in Handler.cs."
            };

        var service =
            new CodeCopilotService(
                retrievalService,
                contextBuilder,
                answerService);

        var response =
            await service.AskAsync(
                new CopilotRequest("Where is the handler?", "owner/repository", "main", 5));

        Assert.Equal(
            "The handler is in Handler.cs.",
            response.Answer);

        Assert.Single(response.Sources);

        Assert.Equal(
            "src/Handler.cs",
            response.Sources[0].Path);

        Assert.Equal(
            10,
            response.Sources[0].StartLine);

        Assert.Equal(
            25,
            response.Sources[0].EndLine);

        Assert.Equal(
            "Where is the handler?",
            answerService.LastQuery);

        Assert.Contains(
            "src/Handler.cs",
            answerService.LastContext);
    }

    [Fact]
    public async Task AskAsync_WithNoResults_DoesNotCallAnswerGeneration()
    {
        var retrievalService =
            new FakeCodeRetrievalService();

        var contextBuilder =
            new CodeContextBuilder(
                Options.Create(
                    new RetrievalContextOptions()));

        var answerService =
            new FakeCodeAnswerGenerationService();

        var service =
            new CodeCopilotService(
                retrievalService,
                contextBuilder,
                answerService);

        var response =
            await service.AskAsync(
                new CopilotRequest("Where is the handler?", "owner/repository", "main", 5));

        Assert.Contains(
            "could not find relevant code",
            response.Answer,
            StringComparison.OrdinalIgnoreCase);

        Assert.Empty(response.Sources);

        Assert.Null(answerService.LastQuery);
    }

    [Fact]
    public async Task AskAsync_WithEmptyQuery_Throws()
    {
        var retrievalService =
            new FakeCodeRetrievalService();

        var contextBuilder =
            new CodeContextBuilder(
                Options.Create(
                    new RetrievalContextOptions()));

        var answerService =
            new FakeCodeAnswerGenerationService();

        var service =
            new CodeCopilotService(
                retrievalService,
                contextBuilder,
                answerService);

        await Assert.ThrowsAsync<ArgumentException>(
            () => service.AskAsync(
                new CopilotRequest(
                    string.Empty,
                    "owner/repository",
                    "main",
                    5)));
    }

    [Fact]
    public async Task AskAsync_WithInvalidTop_Throws()
    {
        var retrievalService =
            new FakeCodeRetrievalService();

        var contextBuilder =
            new CodeContextBuilder(
                Options.Create(
                    new RetrievalContextOptions()));

        var answerService =
            new FakeCodeAnswerGenerationService();

        var service =
            new CodeCopilotService(
                retrievalService,
                contextBuilder,
                answerService);

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => service.AskAsync(
                new CopilotRequest("test", "owner/repository", "main", 0)));
    }
}




