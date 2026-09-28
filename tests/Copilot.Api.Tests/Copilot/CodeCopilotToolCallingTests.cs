using Copilot.Api.Models.Copilot;
using Copilot.Api.Services.Copilot;
using Copilot.Api.Services.Tools;

namespace Copilot.Api.Tests.Copilot;

public sealed class CodeCopilotToolCallingTests
{
    [Fact]
    public async Task AskAsync_UsesToolCallingPipeline_WhenConfigured()
    {
        var toolCallingService = new FakeToolCallingAnswerGenerationService(
            "Tool-calling answer");

        var registry = new FakeToolRegistry(
            new FakeTool("code_search"));

        var legacyAnswerService =
            new FakeAnswerGenerationService("Legacy answer");

        var service = CreateService(
            toolCallingService,
            registry,
            legacyAnswerService);

        var response =
            await service.AskAsync(
                new CopilotRequest(
                    "Where is authentication configured?",
                    "https://github.com/example/repo",
                    "main"));

        Assert.Equal("Tool-calling answer", response.Answer);
        Assert.Equal(1, toolCallingService.CallCount);
        Assert.Equal(
            "Where is authentication configured?",
            toolCallingService.Query);
        Assert.Equal(
            "https://github.com/example/repo",
            toolCallingService.Repository);
        Assert.Equal("main", toolCallingService.Branch);
        Assert.Equal(0, legacyAnswerService.CallCount);
    }

    [Fact]
    public async Task AskAsync_PreservesRequestedBranch_WhenUsingToolCalling()
    {
        var toolCallingService = new FakeToolCallingAnswerGenerationService(
            "Feature branch answer");

        var registry = new FakeToolRegistry(
            new FakeTool("code_search"),
            new FakeTool("file_inspection"));

        var service = CreateService(
            toolCallingService,
            registry,
            new FakeAnswerGenerationService("Legacy answer"));

        var response =
            await service.AskAsync(
                new CopilotRequest(
                    "Explain the payment flow.",
                    "https://github.com/example/repo",
                    "feature/payment"));

        Assert.Equal("Feature branch answer", response.Answer);
        Assert.Equal("feature/payment", toolCallingService.Branch);
        Assert.Equal(2, toolCallingService.Tools.Count);
    }

    [Fact]
    public async Task AskAsync_DoesNotUseLegacyAnswerGeneration_WhenToolCallingIsConfigured()
    {
        var toolCallingService = new FakeToolCallingAnswerGenerationService(
            "Integrated answer");

        var registry = new FakeToolRegistry(
            new FakeTool("code_search"));

        var failingLegacyService = new FailingAnswerGenerationService();

        var service = CreateService(
            toolCallingService,
            registry,
            failingLegacyService);

        var response =
            await service.AskAsync(
                new CopilotRequest(
                    "Find the API endpoint.",
                    "https://github.com/example/repo",
                    "main"));

        Assert.Equal("Integrated answer", response.Answer);
        Assert.Equal(1, toolCallingService.CallCount);
    }

    private static CodeCopilotService CreateService(
        IToolCallingCodeAnswerGenerationService toolCallingService,
        ICopilotToolRegistry registry,
        ICodeAnswerGenerationService legacyAnswerService)
    {
        return new CodeCopilotService(
            new FakeRetrievalService(),
            new FakeContextBuilder(),
            legacyAnswerService,
            toolCallingService,
            registry);
    }

    private sealed class FakeToolCallingAnswerGenerationService
        : IToolCallingCodeAnswerGenerationService
    {
        private readonly string _answer;

        public FakeToolCallingAnswerGenerationService(string answer)
        {
            _answer = answer;
        }

        public int CallCount { get; private set; }

        public string Query { get; private set; } = string.Empty;

        public string Repository { get; private set; } = string.Empty;

        public string Branch { get; private set; } = string.Empty;

        public IReadOnlyCollection<ICopilotTool> Tools { get; private set; }
            = Array.Empty<ICopilotTool>();

        public Task<string> GenerateAsync(
            string query,
            string repository,
            string branch,
            IReadOnlyCollection<ICopilotTool> tools,
            CancellationToken cancellationToken = default)
        {
            CallCount++;
            Query = query;
            Repository = repository;
            Branch = branch;
            Tools = tools;

            return Task.FromResult(_answer);
        }
    }

    private sealed class FakeToolRegistry : ICopilotToolRegistry
    {
        private readonly IReadOnlyCollection<ICopilotTool> _tools;

        public FakeToolRegistry(params ICopilotTool[] tools)
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

        public string Description => "Fake tool for testing.";

        public string ParametersJsonSchema => "{}";

        public Task<string> ExecuteAsync(
            string argumentsJson,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult("{}");
        }
    }

    private sealed class FakeRetrievalService : ICodeRetrievalService
    {
        public Task<CodeRetrievalResponse> RetrieveAsync(
            CodeRetrievalRequest request,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                new CodeRetrievalResponse(
                    request,
                    Array.Empty<CodeRetrievalResult>()));
        }
    }

    private sealed class FakeContextBuilder : ICodeContextBuilder
    {
        public CodeContext Build(
            IReadOnlyList<CodeRetrievalResult> results)
        {
            return new CodeContext(
                Array.Empty<CodeContextChunk>(),
                string.Empty);
        }
    }

    private class FakeAnswerGenerationService
        : ICodeAnswerGenerationService
    {
        private readonly string _answer;

        public FakeAnswerGenerationService(string answer)
        {
            _answer = answer;
        }

        public int CallCount { get; private set; }

        public Task<string> GenerateAsync(
            string query,
            string context,
            CancellationToken cancellationToken = default)
        {
            CallCount++;
            return Task.FromResult(_answer);
        }
    }

    private sealed class FailingAnswerGenerationService
        : FakeAnswerGenerationService
    {
        public FailingAnswerGenerationService()
            : base("This answer must never be returned.")
        {
        }

        public new Task<string> GenerateAsync(
            string query,
            string context,
            CancellationToken cancellationToken = default)
        {
            throw new InvalidOperationException(
                "Legacy answer generation should not be called.");
        }
    }
}