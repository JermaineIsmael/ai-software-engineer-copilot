using Copilot.Api.Models.TestGeneration;
using Copilot.Api.Services.Copilot;
using Copilot.Api.Services.TestGeneration;
using Copilot.Api.Services.Tools;

namespace Copilot.Api.Tests.TestGeneration;

public sealed class TestGenerationServiceTests
{
    [Fact]
    public async Task GenerateAsync_UsesRegisteredToolsAndReturnsGeneratedTests()
    {
        var answerService =
            new FakeToolCallingAnswerGenerationService(
                """
                {
                  "framework": "xUnit",
                  "testCode": "public class CalculatorTests { }",
                  "testCases": [
                    {
                      "name": "Add_ReturnsSum",
                      "scenario": "Adding two numbers",
                      "expectedBehavior": "The sum is returned"
                    }
                  ]
                }
                """);

        var tools = new[]
        {
            new FakeTool("code_search"),
            new FakeTool("file_inspection")
        };

        var registry =
            new CopilotToolRegistry(tools);

        var service =
            new TestGenerationService(
                answerService,
                registry);

        var response =
            await service.GenerateAsync(
                new TestGenerationRequest(
                    "Generate tests for Calculator",
                    "https://github.com/example/repo",
                    "develop"));

        Assert.Equal(
            "https://github.com/example/repo",
            response.Repository);

        Assert.Equal(
            "develop",
            response.Branch);

        Assert.Equal(
            "xUnit",
            response.Framework);

        Assert.Single(response.TestCases);

        Assert.Equal(
            "Add_ReturnsSum",
            response.TestCases[0].Name);

        Assert.Equal(
            "https://github.com/example/repo",
            answerService.Repository);

        Assert.Equal(
            "develop",
            answerService.Branch);

        Assert.Equal(
            2,
            answerService.Tools.Count);
    }

    [Fact]
    public async Task GenerateAsync_PreservesQuery()
    {
        var answerService =
            new FakeToolCallingAnswerGenerationService(
                """
                {
                  "framework": "xUnit",
                  "testCode": "public class Tests { }",
                  "testCases": []
                }
                """);

        var service =
            new TestGenerationService(
                answerService,
                new CopilotToolRegistry(
                    new[] { new FakeTool("code_search") }));

        var response =
            await service.GenerateAsync(
                new TestGenerationRequest(
                    "Generate tests for OrderService",
                    "repo"));

        Assert.Equal(
            "Generate tests for OrderService",
            response.Query);
    }

    [Fact]
    public async Task GenerateAsync_DefaultBranchIsMain()
    {
        var answerService =
            new FakeToolCallingAnswerGenerationService(
                """
                {
                  "framework": "xUnit",
                  "testCode": "public class Tests { }",
                  "testCases": []
                }
                """);

        var service =
            new TestGenerationService(
                answerService,
                new CopilotToolRegistry(
                    new[] { new FakeTool("code_search") }));

        var response =
            await service.GenerateAsync(
                new TestGenerationRequest(
                    "Generate tests",
                    "repo"));

        Assert.Equal(
            "main",
            response.Branch);
    }

    [Fact]
    public async Task GenerateAsync_AllowsEmptyTestCaseList()
    {
        var answerService =
            new FakeToolCallingAnswerGenerationService(
                """
                {
                  "framework": "xUnit",
                  "testCode": "public class Tests { }",
                  "testCases": []
                }
                """);

        var service =
            new TestGenerationService(
                answerService,
                new CopilotToolRegistry(
                    new[] { new FakeTool("code_search") }));

        var response =
            await service.GenerateAsync(
                new TestGenerationRequest(
                    "Generate tests",
                    "repo"));

        Assert.Empty(response.TestCases);
    }

    [Fact]
    public async Task GenerateAsync_ThrowsWhenQueryIsEmpty()
    {
        var service = CreateService();

        await Assert.ThrowsAsync<ArgumentException>(
            () => service.GenerateAsync(
                new TestGenerationRequest(
                    "",
                    "repo")));
    }

    [Fact]
    public async Task GenerateAsync_ThrowsWhenRepositoryIsEmpty()
    {
        var service = CreateService();

        await Assert.ThrowsAsync<ArgumentException>(
            () => service.GenerateAsync(
                new TestGenerationRequest(
                    "Generate tests",
                    "")));
    }

    [Fact]
    public async Task GenerateAsync_ThrowsWhenBranchIsEmpty()
    {
        var service = CreateService();

        await Assert.ThrowsAsync<ArgumentException>(
            () => service.GenerateAsync(
                new TestGenerationRequest(
                    "Generate tests",
                    "repo",
                    "")));
    }

    [Fact]
    public async Task GenerateAsync_ThrowsWhenResponseIsInvalidJson()
    {
        var answerService =
            new FakeToolCallingAnswerGenerationService(
                "not-json");

        var service =
            new TestGenerationService(
                answerService,
                new CopilotToolRegistry(
                    new[] { new FakeTool("code_search") }));

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.GenerateAsync(
                new TestGenerationRequest(
                    "Generate tests",
                    "repo")));
    }

    [Fact]
    public async Task GenerateAsync_ThrowsWhenTestCodeIsMissing()
    {
        var answerService =
            new FakeToolCallingAnswerGenerationService(
                """
                {
                  "framework": "xUnit",
                  "testCases": []
                }
                """);

        var service =
            new TestGenerationService(
                answerService,
                new CopilotToolRegistry(
                    new[] { new FakeTool("code_search") }));

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.GenerateAsync(
                new TestGenerationRequest(
                    "Generate tests",
                    "repo")));
    }

    private static TestGenerationService CreateService()
    {
        return new TestGenerationService(
            new FakeToolCallingAnswerGenerationService(
                """
                {
                  "framework": "xUnit",
                  "testCode": "public class Tests { }",
                  "testCases": []
                }
                """),
            new CopilotToolRegistry(
                new[] { new FakeTool("code_search") }));
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

        public IReadOnlyCollection<ICopilotTool> Tools { get; private set; } =
            Array.Empty<ICopilotTool>();

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

    private sealed class FakeTool : ICopilotTool
    {
        public FakeTool(string name)
        {
            Name = name;
        }

        public string Name { get; }

        public string Description => "Fake tool";

        public string ParametersJsonSchema => "{}";

        public Task<string> ExecuteAsync(
            string argumentsJson,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult("{}");
        }
    }
}