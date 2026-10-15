# Testing Strategy

## Overview

The AI Software Engineer Copilot uses a comprehensive testing strategy combining unit tests, integration tests, and CI/CD validation to ensure quality and reliability.

## Testing Pyramid

```
        ▲
       /|\
      / | \  End-to-End Tests (Few)
     /  |  \ Manual testing, smoke tests
    /   |   \━━━━━━━━━━━━━━━━━━━━━━━━━
   /    |    \
  /     |     \  Integration Tests (Some)
 /      |      \ API endpoints, service interactions
/       |       \━━━━━━━━━━━━━━━━━━━━━━━━━━
        |        
        |        \ Unit Tests (Many)
        |         \ Individual services, methods
        |━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        
Focus: 70-80% unit, 15-25% integration, 5-10% end-to-end
```

## Unit Testing

### Framework and Tools
- **Framework:** xUnit 2.x
- **Mocking:** Moq
- **Assertions:** xUnit built-in
- **Target:** .NET 8.0

### Principles

1. **One Assertion Per Test** (when possible)
   - Each test validates one behavior
   - Multiple assertions only for related validations

2. **Arrange-Act-Assert (AAA) Pattern**
   - Clear test structure
   - Setup → Execute → Verify

3. **Mock External Dependencies**
   - Azure services mocked (Azure OpenAI, AI Search)
   - GitHub API mocked
   - No real external service calls

4. **Test Doubles**
   - Fake implementations: `FakeAzureOpenAIService`, `FakeCodeRetrievalService`
   - Failing implementations: `FailingAzureOpenAIService`
   - Used for integration tests within CI/CD

### Test Organization

```
tests/Copilot.Api.Tests/
├── Services/
│   ├── Copilot/              # Copilot service tests
│   │   ├── CodeCopilotServiceTests.cs
│   │   └── CodeCopilotToolCallingTests.cs
│   ├── Retrieval/            # Retrieval service tests
│   ├── Search/               # Search service tests
│   ├── Repository/           # Repository ingestion tests
│   ├── Tools/                # Tool framework tests
│   └── ...
├── Endpoints/                # API endpoint tests
├── Models/                   # Data model tests
└── [Feature]/                # Feature-specific tests
```

### Test Naming Convention

```
[MethodUnderTest]_[Scenario]_[ExpectedResult]
```

**Examples:**
- `AskAsync_WithValidRequest_ReturnsResponse`
- `RetrieveAsync_WithEmptyRepository_ThrowsArgumentException`
- `VectorSearchAsync_WithValidQuery_ReturnsResults`

### Test Example

```csharp
[Fact]
public async Task AskAsync_WithValidRequest_ReturnsResponse()
{
    // Arrange
    var mockRetrieval = new Mock<ICodeRetrievalService>();
    mockRetrieval
        .Setup(x => x.RetrieveAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<int>(),
            It.IsAny<CancellationToken>()))
        .ReturnsAsync(new CodeRetrievalResponse { /* ... */ });
    
    var service = new CodeCopilotService(
        mockRetrieval.Object,
        new Mock<ICodeContextBuilder>().Object,
        new Mock<ICodeAnswerGenerationService>().Object);
    
    var request = new CopilotRequest
    {
        Query = "test query",
        Repository = "owner/repo",
        Branch = "main",
        Top = 5
    };
    
    // Act
    var response = await service.AskAsync(request);
    
    // Assert
    Assert.NotNull(response);
    mockRetrieval.Verify(x => x.RetrieveAsync(
        It.IsAny<string>(),
        It.IsAny<string>(),
        It.IsAny<string>(),
        It.IsAny<int>(),
        It.IsAny<CancellationToken>()), Times.Once);
}
```

## Integration Testing

### Approach
- Test complete request→response flows
- Use `ApiFactory` for creating test clients
- Services still use fake implementations (not real Azure)
- Verify HTTP status codes and response content

### Test Factory Pattern

```csharp
public class ApiFactory
{
    public HttpClient CreateClient()
    {
        var webApplicationFactory = new WebApplicationFactory<Program>();
        return webApplicationFactory.CreateClient();
    }
}
```

### Integration Test Example

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
    Assert.NotNull(content.Content);
}
```

## Test Categories by Feature

### 1. Azure OpenAI Service Tests

**File:** `AzureOpenAIServiceTests.cs`, `AzureOpenAIEmbeddingServiceTests.cs`

**Coverage:**
- Chat completion with valid input
- Streaming responses
- Embedding generation
- Error handling (rate limits, auth failures)
- Token limit handling

**Fake Implementation:** `FakeAzureOpenAIService`

### 2. Repository Ingestion Tests

**Files:** `RepositoryIngestionServiceTests.cs`, `SourceFileClassifierTests.cs`, `CodeChunkingServiceTests.cs`

**Coverage:**
- File classification (language detection)
- Metadata extraction
- Code chunking with correct boundaries
- Embedding generation for chunks
- Error handling for unsupported files

### 3. Search and Retrieval Tests

**Files:** `CodeRetrievalServiceTests.cs`, `HybridSearchServiceTests.cs`, `VectorSearchServiceTests.cs`

**Coverage:**
- Query embedding generation
- Vector search correctness
- Keyword search functionality
- Hybrid search result merging
- Score normalization
- Context building

**Fake Implementation:** `FakeCodeRetrievalService`, `FakeRetrievalResultProcessor`

### 4. Copilot Service Tests

**Files:** `CodeCopilotServiceTests.cs`, `CodeCopilotToolCallingTests.cs`

**Coverage:**
- Input validation
- Retrieval coordination
- Context building
- Answer generation
- Tool calling (when available)
- Error scenarios

**Fake Implementation:** `FakeCodeAnswerGenerationService`

### 5. Tool Framework Tests

**Files:** `CodeSearchToolTests.cs`, `FileInspectionToolTests.cs`, `CopilotToolRegistryTests.cs`

**Coverage:**
- Tool registration
- Tool execution
- Tool result processing
- Tool discovery via registry

### 6. API Endpoint Tests

**Files:** `ChatEndpointTests.cs`, `ChatEndpointFailureTests.cs`, and others

**Coverage:**
- Request validation
- Success responses (HTTP 200)
- Error responses (HTTP 400, 500)
- Status code correctness
- Response format validation

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

### Run Tests Matching Pattern
```bash
dotnet test --filter "FullyQualifiedName~CodeCopilotServiceTests"
```

### Run with Code Coverage
```bash
dotnet test /p:CollectCoverage=true /p:CoverageFormat=opencover
```

### Run Single Test
```bash
dotnet test --filter "FullyQualifiedName~CodeCopilotServiceTests.AskAsync_WithValidRequest_ReturnsResponse"
```

## Continuous Integration Testing

### GitHub Actions Workflow

**File:** `.github/workflows/ci.yml`

**Stages:**
1. Checkout repository
2. Setup .NET SDK (8.0)
3. Restore dependencies: `dotnet restore`
4. Build (Release config): `dotnet build --configuration Release`
5. Run tests: `dotnet test --configuration Release`
6. Setup Node.js
7. Install frontend dependencies: `npm ci`
8. Build frontend: `npm run build`

**Triggers:**
- Push to `main` or `develop` branches
- Pull requests to `main` or `develop`

**Failure Behavior:**
- Test failures block PR merging
- Build failures block PR merging
- Frontend build failures block PR merging

## Testing Best Practices

### Do's ✓

1. ✓ One assertion per test (or related assertions)
2. ✓ Descriptive test names that explain what is being tested
3. ✓ Mock all external dependencies
4. ✓ Use `It.IsAny<T>()` for flexible matching
5. ✓ Test both success and failure paths
6. ✓ Test boundary conditions (empty, null, zero, max values)
7. ✓ Use `[Theory]` with `[InlineData]` for parameterized tests
8. ✓ Clean up resources (files, connections) after tests

### Don'ts ✗

1. ✗ Don't test implementation details; test behavior
2. ✗ Don't have tests depend on execution order
3. ✗ Don't hardcode file paths or configuration
4. ✗ Don't make real network calls in tests
5. ✗ Don't create interdependent tests
6. ✗ Don't ignore test failures
7. ✗ Don't test external libraries (assume they work)
8. ✗ Don't add secrets or sensitive data to tests

## Known Test Gaps

(To be identified during implementation)

## Testing Checklist for New Features

Before submitting a pull request:

- [ ] All public methods have corresponding unit tests
- [ ] Error scenarios are tested (invalid input, service failures)
- [ ] Async methods test cancellation handling
- [ ] Integration tests verify end-to-end flow
- [ ] Fake implementations exist for external dependencies
- [ ] All tests pass: `dotnet test`
- [ ] Code builds in Release mode: `dotnet build -c Release`
- [ ] No secrets or sensitive data in tests
- [ ] Test names clearly describe scenario and expectation
- [ ] Code coverage for new code is >80%

## Future Enhancements

1. **Performance Testing:** Load tests for concurrent queries
2. **Chaos Testing:** Test failure scenarios systematically
3. **Security Testing:** OWASP vulnerability scanning
4. **UI Testing:** Frontend component and integration tests
5. **Contract Testing:** API contract validation
6. **Mutation Testing:** Verify test quality

## Related Documents

- [Testing Instructions](../../.github/instructions/tests.instructions.md)
- [Coding Standards](coding-standards.md)
- [Global Instructions](../../.github/copilot-instructions.md)
