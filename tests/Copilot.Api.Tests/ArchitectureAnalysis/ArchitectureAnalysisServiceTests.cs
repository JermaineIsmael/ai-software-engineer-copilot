using Copilot.Api.Models.ArchitectureAnalysis;
using Copilot.Api.Services.ArchitectureAnalysis;
using Copilot.Api.Services.Copilot;
using Copilot.Api.Services.Tools;

namespace Copilot.Api.Tests.ArchitectureAnalysis;

public sealed class ArchitectureAnalysisServiceTests
{
    [Fact]
    public async Task AnalyzeAsync_UsesRegisteredToolsAndReturnsAnalysis()
    {
        var tool = new FakeTool();

        var answerService =
            new FakeToolCallingCodeAnswerGenerationService(
                """
                {
                  "summary": "Layered API architecture.",
                  "components": [
                    {
                      "name": "API",
                      "responsibility": "HTTP endpoints",
                      "dependencies": ["Application services"]
                    }
                  ],
                  "dataFlows": [
                    "HTTP request flows through the API into application services."
                  ],
                  "integrationPoints": [
                    "Repository services integrate with external systems."
                  ],
                  "architecturalPatterns": [
                    "Layered architecture"
                  ],
                  "risks": [
                    {
                      "severity": "Medium",
                      "area": "Coupling",
                      "description": "Some components may be tightly coupled.",
                      "recommendation": "Introduce clearer service boundaries."
                    }
                  ],
                  "recommendations": [
                    "Keep application and infrastructure responsibilities separated."
                  ]
                }
                """);

        var registry =
            new FakeToolRegistry(tool);

        var service =
            new ArchitectureAnalysisService(
                answerService,
                registry);

        var result =
            await service.AnalyzeAsync(
                new ArchitectureAnalysisRequest(
                    "Analyze the API architecture.",
                    "https://github.com/example/repository",
                    "main"));

        Assert.Equal(
            "Layered API architecture.",
            result.Summary);

        Assert.Single(result.Components);
        Assert.Single(result.Risks);
        Assert.Single(result.Recommendations);

        Assert.Same(tool, answerService.Tools.Single());
    }

    [Fact]
    public async Task AnalyzeAsync_PreservesRequestedBranch()
    {
        var answerService =
            new FakeToolCallingCodeAnswerGenerationService(
                """
                {
                  "summary": "Architecture summary.",
                  "components": [],
                  "dataFlows": [],
                  "integrationPoints": [],
                  "architecturalPatterns": [],
                  "risks": [],
                  "recommendations": []
                }
                """);

        var service =
            new ArchitectureAnalysisService(
                answerService,
                new FakeToolRegistry(new FakeTool()));

        var result =
            await service.AnalyzeAsync(
                new ArchitectureAnalysisRequest(
                    "Analyze architecture.",
                    "https://github.com/example/repository",
                    "feature/test"));

        Assert.Equal("feature/test", result.Branch);
        Assert.Equal("feature/test", answerService.Branch);
    }

    [Fact]
    public async Task AnalyzeAsync_DefaultsBranchToMain()
    {
        var answerService =
            new FakeToolCallingCodeAnswerGenerationService(
                """
                {
                  "summary": "Architecture summary.",
                  "components": [],
                  "dataFlows": [],
                  "integrationPoints": [],
                  "architecturalPatterns": [],
                  "risks": [],
                  "recommendations": []
                }
                """);

        var service =
            new ArchitectureAnalysisService(
                answerService,
                new FakeToolRegistry(new FakeTool()));

        var result =
            await service.AnalyzeAsync(
                new ArchitectureAnalysisRequest(
                    "Analyze architecture.",
                    "https://github.com/example/repository"));

        Assert.Equal("main", result.Branch);
        Assert.Equal("main", answerService.Branch);
    }

    [Fact]
    public async Task AnalyzeAsync_AllowsEmptyCollections()
    {
        var answerService =
            new FakeToolCallingCodeAnswerGenerationService(
                """
                {
                  "summary": "No significant architecture findings.",
                  "components": [],
                  "dataFlows": [],
                  "integrationPoints": [],
                  "architecturalPatterns": [],
                  "risks": [],
                  "recommendations": []
                }
                """);

        var service =
            new ArchitectureAnalysisService(
                answerService,
                new FakeToolRegistry(new FakeTool()));

        var result =
            await service.AnalyzeAsync(
                new ArchitectureAnalysisRequest(
                    "Analyze architecture.",
                    "https://github.com/example/repository"));

        Assert.Empty(result.Components);
        Assert.Empty(result.DataFlows);
        Assert.Empty(result.IntegrationPoints);
        Assert.Empty(result.ArchitecturalPatterns);
        Assert.Empty(result.Risks);
        Assert.Empty(result.Recommendations);
    }

    [Fact]
    public async Task AnalyzeAsync_ThrowsWhenQueryIsEmpty()
    {
        var service =
            new ArchitectureAnalysisService(
                new FakeToolCallingCodeAnswerGenerationService("{}"),
                new FakeToolRegistry());

        await Assert.ThrowsAsync<ArgumentException>(
            () => service.AnalyzeAsync(
                new ArchitectureAnalysisRequest(
                    "",
                    "https://github.com/example/repository")));
    }

    [Fact]
    public async Task AnalyzeAsync_ThrowsWhenRepositoryIsEmpty()
    {
        var service =
            new ArchitectureAnalysisService(
                new FakeToolCallingCodeAnswerGenerationService("{}"),
                new FakeToolRegistry());

        await Assert.ThrowsAsync<ArgumentException>(
            () => service.AnalyzeAsync(
                new ArchitectureAnalysisRequest(
                    "Analyze architecture.",
                    "")));
    }

    [Fact]
    public async Task AnalyzeAsync_ThrowsWhenBranchIsEmpty()
    {
        var service =
            new ArchitectureAnalysisService(
                new FakeToolCallingCodeAnswerGenerationService("{}"),
                new FakeToolRegistry());

        await Assert.ThrowsAsync<ArgumentException>(
            () => service.AnalyzeAsync(
                new ArchitectureAnalysisRequest(
                    "Analyze architecture.",
                    "https://github.com/example/repository",
                    "")));
    }

    [Fact]
    public async Task AnalyzeAsync_ThrowsWhenResponseIsInvalidJson()
    {
        var service =
            new ArchitectureAnalysisService(
                new FakeToolCallingCodeAnswerGenerationService(
                    "not valid json"),
                new FakeToolRegistry());

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.AnalyzeAsync(
                new ArchitectureAnalysisRequest(
                    "Analyze architecture.",
                    "https://github.com/example/repository")));
    }

    [Fact]
    public async Task AnalyzeAsync_ThrowsWhenSummaryIsMissing()
    {
        var service =
            new ArchitectureAnalysisService(
                new FakeToolCallingCodeAnswerGenerationService(
                    """
                    {
                      "components": [],
                      "dataFlows": [],
                      "integrationPoints": [],
                      "architecturalPatterns": [],
                      "risks": [],
                      "recommendations": []
                    }
                    """),
                new FakeToolRegistry());

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.AnalyzeAsync(
                new ArchitectureAnalysisRequest(
                    "Analyze architecture.",
                    "https://github.com/example/repository")));
    }

    [Fact]
    public async Task AnalyzeAsync_ThrowsWhenComponentPropertiesAreMissing()
    {
        var service =
            new ArchitectureAnalysisService(
                new FakeToolCallingCodeAnswerGenerationService(
                    """
                    {
                      "summary": "Architecture summary.",
                      "components": [
                        {
                          "name": "API"
                        }
                      ],
                      "dataFlows": [],
                      "integrationPoints": [],
                      "architecturalPatterns": [],
                      "risks": [],
                      "recommendations": []
                    }
                    """),
                new FakeToolRegistry());

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.AnalyzeAsync(
                new ArchitectureAnalysisRequest(
                    "Analyze architecture.",
                    "https://github.com/example/repository")));
    }

    private sealed class FakeToolCallingCodeAnswerGenerationService
        : IToolCallingCodeAnswerGenerationService
    {
        private readonly string _response;

        public FakeToolCallingCodeAnswerGenerationService(
            string response)
        {
            _response = response;
        }

        public IReadOnlyCollection<ICopilotTool> Tools { get; private set; } =
            Array.Empty<ICopilotTool>();

        public string Branch { get; private set; } = string.Empty;

        public Task<string> GenerateAsync(
            string query,
            string repository,
            string branch,
            IReadOnlyCollection<ICopilotTool> tools,
            CancellationToken cancellationToken = default)
        {
            Tools = tools;
            Branch = branch;

            return Task.FromResult(_response);
        }
    }

    private sealed class FakeToolRegistry : ICopilotToolRegistry
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
        public string Name => "test_tool";

        public string Description => "Test tool.";

        public string ParametersJsonSchema => "{}";

        public Task<string> ExecuteAsync(
            string argumentsJson,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult("{}");
        }
    }
}