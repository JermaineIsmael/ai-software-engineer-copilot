using Copilot.Api.Models.CodeReview;
using Copilot.Api.Services.CodeReview;
using Copilot.Api.Services.Copilot;
using Copilot.Api.Services.Tools;

namespace Copilot.Api.Tests.CodeReview;

public sealed class CodeReviewServiceTests
{
    [Fact]
    public async Task ReviewAsync_UsesRegisteredToolsAndReturnsFindings()
    {
        var tool =
            new FakeTool(
                "code_search");

        var registry =
            new FakeToolRegistry(tool);

        var generationService =
            new FakeToolCallingAnswerGenerationService(
                """
                {
                  "findings": [
                    {
                      "severity": "High",
                      "file": "src/Auth.cs",
                      "location": "ValidateToken",
                      "finding": "Token validation does not verify expiration.",
                      "explanation": "Expired tokens could remain accepted.",
                      "recommendation": "Validate the token expiration claim."
                    }
                  ]
                }
                """);

        var service =
            new CodeReviewService(
                generationService,
                registry);

        var response =
            await service.ReviewAsync(
                new CodeReviewRequest(
                    "Review authentication for security issues.",
                    "https://github.com/example/repo",
                    "main"));

        Assert.Single(response.Findings);

        var finding =
            response.Findings[0];

        Assert.Equal("High", finding.Severity);
        Assert.Equal("src/Auth.cs", finding.File);
        Assert.Equal("ValidateToken", finding.Location);
        Assert.Contains("expiration", finding.Finding);
        Assert.Contains("Expired", finding.Explanation);
        Assert.Contains("Validate", finding.Recommendation);

        Assert.Equal(
            "https://github.com/example/repo",
            generationService.Repository);

        Assert.Equal(
            "main",
            generationService.Branch);

        Assert.Single(
            generationService.Tools);
    }

    [Fact]
    public async Task ReviewAsync_PreservesRequestedBranch()
    {
        var generationService =
            new FakeToolCallingAnswerGenerationService(
                """
                {
                  "findings": []
                }
                """);

        var service =
            new CodeReviewService(
                generationService,
                new FakeToolRegistry());

        var response =
            await service.ReviewAsync(
                new CodeReviewRequest(
                    "Review this repository.",
                    "https://github.com/example/repo",
                    "develop"));

        Assert.Equal("develop", response.Branch);
        Assert.Equal("develop", generationService.Branch);
    }

    [Fact]
    public async Task ReviewAsync_ReturnsEmptyFindingsWhenNoIssuesExist()
    {
        var service =
            new CodeReviewService(
                new FakeToolCallingAnswerGenerationService(
                    """
                    {
                      "findings": []
                    }
                    """),
                new FakeToolRegistry());

        var response =
            await service.ReviewAsync(
                new CodeReviewRequest(
                    "Review the repository.",
                    "https://github.com/example/repo"));

        Assert.Empty(response.Findings);
    }

    [Fact]
    public async Task ReviewAsync_ThrowsWhenQueryIsEmpty()
    {
        var service =
            new CodeReviewService(
                new FakeToolCallingAnswerGenerationService("{}"),
                new FakeToolRegistry());

        await Assert.ThrowsAsync<ArgumentException>(
            () => service.ReviewAsync(
                new CodeReviewRequest(
                    "",
                    "https://github.com/example/repo")));
    }

    [Fact]
    public async Task ReviewAsync_ThrowsWhenRepositoryIsEmpty()
    {
        var service =
            new CodeReviewService(
                new FakeToolCallingAnswerGenerationService("{}"),
                new FakeToolRegistry());

        await Assert.ThrowsAsync<ArgumentException>(
            () => service.ReviewAsync(
                new CodeReviewRequest(
                    "Review this.",
                    "")));
    }

    [Fact]
    public async Task ReviewAsync_ThrowsWhenBranchIsEmpty()
    {
        var service =
            new CodeReviewService(
                new FakeToolCallingAnswerGenerationService("{}"),
                new FakeToolRegistry());

        await Assert.ThrowsAsync<ArgumentException>(
            () => service.ReviewAsync(
                new CodeReviewRequest(
                    "Review this.",
                    "https://github.com/example/repo",
                    "")));
    }

    [Fact]
    public async Task ReviewAsync_ThrowsWhenResponseIsInvalidJson()
    {
        var service =
            new CodeReviewService(
                new FakeToolCallingAnswerGenerationService(
                    "not valid json"),
                new FakeToolRegistry());

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.ReviewAsync(
                new CodeReviewRequest(
                    "Review this.",
                    "https://github.com/example/repo")));
    }

    [Fact]
    public async Task ReviewAsync_ThrowsWhenFindingsPropertyIsMissing()
    {
        var service =
            new CodeReviewService(
                new FakeToolCallingAnswerGenerationService(
                    """
                    {
                      "review": []
                    }
                    """),
                new FakeToolRegistry());

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.ReviewAsync(
                new CodeReviewRequest(
                    "Review this.",
                    "https://github.com/example/repo")));
    }

    private sealed class FakeToolCallingAnswerGenerationService
        : IToolCallingCodeAnswerGenerationService
    {
        private readonly string _response;

        public FakeToolCallingAnswerGenerationService(
            string response)
        {
            _response = response;
        }

        public string Repository { get; private set; } = string.Empty;

        public string Branch { get; private set; } = string.Empty;

        public IReadOnlyCollection<ICopilotTool> Tools { get; private set; }
            = [];

        public Task<string> GenerateAsync(
            string query,
            string repository,
            string branch,
            IReadOnlyCollection<ICopilotTool> tools,
            CancellationToken cancellationToken = default)
        {
            Repository = repository;
            Branch = branch;
            Tools = tools;

            return Task.FromResult(_response);
        }
    }

    private sealed class FakeToolRegistry
        : ICopilotToolRegistry
    {
        private readonly IReadOnlyCollection<ICopilotTool> _tools;

        public FakeToolRegistry(
            params ICopilotTool[] tools)
        {
            _tools = tools;
        }

        public IReadOnlyCollection<ICopilotTool> GetTools()
        {
            return _tools;
        }

        public ICopilotTool? GetTool(string name)
        {
            return _tools.FirstOrDefault(
                tool => string.Equals(
                    tool.Name,
                    name,
                    StringComparison.OrdinalIgnoreCase));
        }
    }

    private sealed class FakeTool : ICopilotTool
    {
        public FakeTool(string name)
        {
            Name = name;
        }

        public string Name { get; }

        public string Description =>
            "Fake test tool.";

        public string ParametersJsonSchema =>
            """
            {
              "type": "object"
            }
            """;

        public Task<string> ExecuteAsync(
            string argumentsJson,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult("{}");
        }
    }
}
