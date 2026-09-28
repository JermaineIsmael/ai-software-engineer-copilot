using Copilot.Api.Models.Retrieval;
using Copilot.Api.Services.Retrieval;
using Copilot.Api.Services.Tools;

namespace Copilot.Api.Tests.Tools;

public sealed class CodeSearchToolTests
{
    [Fact]
    public async Task ExecuteAsync_ExecutesRetrievalAndReturnsJson()
    {
        var retrievalService =
            new FakeCodeRetrievalService();

        var tool =
            new CodeSearchTool(retrievalService);

        var result =
            await tool.ExecuteAsync(
                """
                {
                  "query": "OrderService",
                  "repository": "https://github.com/example/repository",
                  "branch": "develop",
                  "top": 3
                }
                """);

        Assert.Contains(
            "OrderService",
            result);

        Assert.Equal(
            "OrderService",
            retrievalService.Request?.Query);

        Assert.Equal(
            "https://github.com/example/repository",
            retrievalService.Request?.Repository);

        Assert.Equal(
            "develop",
            retrievalService.Request?.Branch);

        Assert.Equal(
            3,
            retrievalService.Request?.Top);
    }

    [Fact]
    public async Task ExecuteAsync_UsesDefaultBranchAndTop()
    {
        var retrievalService =
            new FakeCodeRetrievalService();

        var tool =
            new CodeSearchTool(retrievalService);

        await tool.ExecuteAsync(
            """
            {
              "query": "OrderService",
              "repository": "https://github.com/example/repository"
            }
            """);

        Assert.Equal(
            "main",
            retrievalService.Request?.Branch);

        Assert.Equal(
            5,
            retrievalService.Request?.Top);
    }

    [Fact]
    public async Task ExecuteAsync_RejectsInvalidTop()
    {
        var tool =
            new CodeSearchTool(
                new FakeCodeRetrievalService());

        await Assert.ThrowsAsync<ArgumentException>(
            () =>
                tool.ExecuteAsync(
                    """
                    {
                      "query": "OrderService",
                      "repository": "https://github.com/example/repository",
                      "top": 21
                    }
                    """));
    }

    private sealed class FakeCodeRetrievalService :
        ICodeRetrievalService
    {
        public CodeRetrievalRequest? Request { get; private set; }

        public Task<CodeRetrievalResponse> RetrieveAsync(
            CodeRetrievalRequest request,
            CancellationToken cancellationToken = default)
        {
            Request = request;

            return Task.FromResult(
                new CodeRetrievalResponse(
                    request.Query,
                    []));
        }
    }
}
