---
applyTo: ["tests/Copilot.Api.Tests/**/*.cs"]
description: Testing strategy and conventions for the Copilot API test suite
---

# Testing Instructions

This file documents the testing framework, practices, and conventions used in the Copilot API test suite.

**Applies to:** `tests/Copilot.Api.Tests/**/*.cs`

## Testing Framework

- **Unit Test Framework:** xUnit 2.x
- **Mocking Framework:** Moq
- **Integration Testing:** ASP.NET Core Testing Framework (`Microsoft.AspNetCore.Mvc.Testing`)
- **Test Assertions:** xUnit built-in assertions
- **.NET Target:** net8.0 (same as API)

## Test Project Structure

```
tests/Copilot.Api.Tests/
├── ApiFactory.cs                            # Integration test factory
├── FailingApiFactory.cs                     # Factory for failure scenarios
├── FakeAzureOpenAIService.cs                # Fake AI service for testing
├── FailingAzureOpenAIService.cs             # Failing AI service for error tests
├── AzureOpenAIServiceTests.cs               # Unit tests for AI service
├── AzureOpenAIEmbeddingServiceTests.cs      # Unit tests for embeddings
├── Copilot/                                 # Copilot feature tests
│   ├── CodeCopilotServiceTests.cs
│   ├── CodeCopilotToolCallingTests.cs
│   ├── CopilotPromptBuilderTests.cs
│   ├── FakeCodeAnswerGenerationService.cs
│   └── ...
├── Retrieval/                               # Retrieval feature tests
│   ├── CodeRetrievalServiceTests.cs
│   ├── CodeContextBuilderTests.cs
│   ├── FakeRetrievalResultProcessor.cs
│   └── ...
├── Search/                                  # Search feature tests
│   ├── HybridSearchServiceTests.cs
│   ├── VectorSearchServiceTests.cs
│   └── ...
├── Tools/                                   # Tool tests
│   ├── CodeSearchToolTests.cs
│   ├── CopilotToolRegistryTests.cs
│   └── ...
└── [Feature]/                               # Feature-specific tests
    └── ...
```

## Test Naming Conventions

Test classes and methods follow a consistent naming pattern for clarity:

```csharp
[MethodUnderTest]_[Scenario]_[ExpectedResult]
```

**Examples:**
- `AskAsync_WithValidRequest_ReturnsResponse`
- `RetrieveAsync_WithEmptyRepository_ThrowsArgumentException`
- `AskAsync_WhenAIServiceFails_ReturnsErrorResponse`
- `VectorSearchAsync_WithValidQuery_ReturnsResults`

**Test class naming:**
- `XxxServiceTests` for service classes
- `XxxEndpointTests` for endpoint handlers
- `XxxResponseTests` for model serialization/validation

## Unit Testing Patterns

### 1. Arrange-Act-Assert (AAA)

All unit tests follow the AAA pattern:

```csharp
[Fact]
public async Task AskAsync_WithValidRequest_ReturnsResponse()
{
    // Arrange: Set up test data and mocks
    var mockService = new Mock<ICodeRetrievalService>();
    mockService
        .Setup(x => x.RetrieveAsync(
            It.IsAny<string>(), 
            It.IsAny<string>(), 
            It.IsAny<string>(), 
            It.IsAny<int>(), 
            It.IsAny<CancellationToken>()))
        .ReturnsAsync(new CodeRetrievalResponse { /* ... */ });
    
    var copilotService = new CodeCopilotService(mockService.Object);
    var request = new CopilotRequest { Query = "test", Repository = "owner/repo", Branch = "main", Top = 5 };
    
    // Act: Execute the code being tested
    var response = await copilotService.AskAsync(request);
    
    // Assert: Verify the results
    Assert.NotNull(response);
    Assert.Equal("test response", response.Content);
}
```

### 2. Mocking External Dependencies

Use Moq to mock external services:

```csharp
var mockAiService = new Mock<IAzureOpenAIService>();
mockAiService
    .Setup(x => x.ChatAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
    .ReturnsAsync("mocked response");

var service = new CodeReviewService(mockAiService.Object);
```

### 3. Testing Exception Scenarios

Test both success and failure paths:

```csharp
[Fact]
public async Task AskAsync_WhenRepositoryNotFound_ThrowsArgumentException()
{
    // Arrange
    var mockProvider = new Mock<IRepositoryProvider>();
    mockProvider
        .Setup(x => x.GetFilesAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
        .ThrowsAsync(new ArgumentException("Repository not found"));
    
    var service = new RepositoryIngestionService(mockProvider.Object);
    
    // Act & Assert
    await Assert.ThrowsAsync<ArgumentException>(
        () => service.IngestAsync("nonexistent", "main", CancellationToken.None));
}
```

### 4. Testing Async Methods

Always use `async Task` in test methods:

```csharp
[Fact]
public async Task MethodAsync_Scenario_Expected()
{
    // Test async methods
    var result = await service.GetAsync(cancellationToken: default);
    Assert.NotNull(result);
}
```

### 5. Testing Cancellation

Test cancellation token handling:

```csharp
[Fact]
public async Task GetAsync_WithCancelledToken_ThrowsOperationCanceledException()
{
    var cts = new CancellationTokenSource();
    cts.Cancel();
    
    await Assert.ThrowsAsync<OperationCanceledException>(
        () => service.GetAsync(cts.Token));
}
```

## Integration Testing Patterns

### Using ApiFactory

Integrate tests should use the `ApiFactory` to test complete request-response flows:

```csharp
[Fact]
public async Task CopilotEndpoint_WithValidRequest_ReturnsOk()
{
    // Arrange
    using var factory = new ApiFactory();
    using var client = factory.CreateClient();
    
    var request = new CopilotRequest 
    { 
        Query = "test", 
        Repository = "owner/repo", 
        Branch = "main", 
        Top = 5 
    };
    
    // Act
    var response = await client.PostAsJsonAsync("/api/copilot/ask", request);
    var content = await response.Content.ReadAsAsync<CopilotResponse>();
    
    // Assert
    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    Assert.NotNull(content);
}
```

### Testing Error Responses

Use `FailingApiFactory` or `FailingAzureOpenAIService` for error scenario testing:

```csharp
[Fact]
public async Task CopilotEndpoint_WhenAIServiceFails_ReturnsBadRequest()
{
    // Arrange
    using var factory = new FailingApiFactory();
    using var client = factory.CreateClient();
    
    var request = new CopilotRequest { /* ... */ };
    
    // Act
    var response = await client.PostAsJsonAsync("/api/copilot/ask", request);
    
    // Assert
    Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
}
```

## Test Data and Fixtures

### Fake Implementations

Create fake implementations for services used across multiple tests:

```csharp
public class FakeAzureOpenAIService : IAzureOpenAIService
{
    public async Task<string> ChatAsync(string message, CancellationToken cancellationToken = default)
    {
        return "Fake AI response";
    }
    
    public async IAsyncEnumerable<string> ChatStreamAsync(
        string message, 
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        yield return "Fake";
        yield return " streaming";
        yield return " response";
    }
}
```

### Test Constants and Helper Methods

Define reusable test data:

```csharp
public static class TestData
{
    public const string DefaultRepository = "owner/repo";
    public const string DefaultBranch = "main";
    
    public static CopilotRequest CreateValidCopilotRequest()
    {
        return new CopilotRequest 
        { 
            Query = "test query", 
            Repository = DefaultRepository, 
            Branch = DefaultBranch, 
            Top = 5 
        };
    }
}
```

## Build and Test Commands

### Run All Tests

```bash
dotnet test
```

### Run Tests with Release Configuration

```bash
dotnet test --configuration Release
```

### Run Specific Test Class

```bash
dotnet test tests/Copilot.Api.Tests/Services/Copilot/CodeCopilotServiceTests.cs
```

### Run Tests Matching a Filter

```bash
dotnet test --filter "FullyQualifiedName~CodeCopilotServiceTests"
```

### Run with Coverage

```bash
dotnet test --collect:"XPlat Code Coverage"
```

## Test Organization Guidelines

1. **One Assertion Per Test (When Possible)**
   - Each test should verify one specific behavior
   - Use multiple assertions only for related validations on the same result

2. **Test One Scenario Per Test Method**
   - Don't combine multiple scenarios in a single test
   - Name each test to reflect the exact scenario

3. **Keep Tests Independent**
   - Tests should not depend on other tests' execution order
   - Use local test data; avoid shared state
   - Clean up resources after each test if needed

4. **Test Public APIs Only**
   - Unit tests should test public service interfaces
   - Internal implementation details can be tested through public methods

5. **Organize by Feature Area**
   - Group tests into feature directories: `Copilot/`, `Retrieval/`, `Search/`, etc.
   - Keep directory structure parallel to source code structure

## Regression Test Expectations

The following categories should have comprehensive test coverage:

1. **Azure OpenAI Service**
   - Chat completion with valid input
   - Streaming responses
   - Error handling and rate limits
   - Token limit handling

2. **Repository Ingestion**
   - File classification
   - Metadata extraction
   - Code chunking
   - Embedding generation

3. **Code Retrieval and Search**
   - Query embedding
   - Vector search results
   - Hybrid search (vector + keyword)
   - Context building and formatting

4. **Copilot Service**
   - Request validation
   - Tool calling
   - Answer generation
   - Error scenarios

5. **API Endpoints**
   - Valid request handling
   - Input validation
   - Error response formatting
   - Status code correctness

6. **Tool Framework**
   - Tool registration
   - Tool execution
   - Tool result processing

## Continuous Integration Testing

GitHub Actions runs the following tests automatically on push and PR:

1. .NET dependency restore
2. Build in Release configuration
3. All xUnit tests (`dotnet test`)
4. Frontend build validation (npm build)

**See:** `.github/workflows/ci.yml`

## Testing Checklist for New Features

Before submitting a pull request with new functionality:

- [ ] All new public methods have corresponding unit tests
- [ ] Error scenarios are tested (invalid input, service failures)
- [ ] Async methods test cancellation handling
- [ ] Integration tests verify end-to-end flow
- [ ] Fake implementations exist for external dependencies
- [ ] All tests pass: `dotnet test`
- [ ] Code builds in Release mode: `dotnet build -c Release`
- [ ] No secrets or sensitive data in tests or test data

## Known Test Gaps

(To be identified during implementation)

## Links

- [Global Instructions](../copilot-instructions.md)
- [Backend Instructions](backend.instructions.md)
- [Testing Strategy](../../docs/development/testing-strategy.md)
- [Local Development Runbook](../../docs/runbooks/local-development.md)
