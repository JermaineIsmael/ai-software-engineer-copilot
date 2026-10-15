---
applyTo: ["src/backend/**/*.cs", "tests/Copilot.Api.Tests/**/*.cs"]
description: Backend-specific instructions for .NET and ASP.NET Core development
---

# Backend Development Instructions

This file provides guidance for working with the backend ASP.NET Core API and service layer.

**Applies to:** `src/backend/**/*.cs`, `tests/Copilot.Api.Tests/**/*.cs`

## Project Structure

```
src/backend/Copilot.Api/
├── Program.cs                           # Application entry, service registration
├── Endpoints/                           # HTTP endpoint handlers
│   ├── CopilotEndpoints.cs
│   ├── RetrievalEndpoints.cs
│   ├── CodeReviewEndpoints.cs
│   ├── ArchitectureAnalysisEndpoints.cs
│   ├── FileInspectionEndpoints.cs
│   └── TestGenerationEndpoints.cs
├── Services/                            # Business logic, organized by feature
│   ├── AzureOpenAIService.cs            # Core AI service
│   ├── IAzureOpenAIService.cs           # AI service interface
│   ├── Copilot/                         # Copilot orchestration
│   │   ├── ICodeCopilotService.cs
│   │   ├── CodeCopilotService.cs
│   │   ├── ICodeAnswerGenerationService.cs
│   │   ├── AzureOpenAICodeAnswerGenerationService.cs
│   │   ├── IToolCallingCodeAnswerGenerationService.cs
│   │   ├── AzureOpenAIToolCallingCodeAnswerGenerationService.cs
│   │   └── CopilotPromptBuilder.cs
│   ├── Repository/                      # Repository access and ingestion
│   │   ├── IRepositoryProvider.cs
│   │   ├── GitHubRepositoryProvider.cs
│   │   ├── IRepositoryIngestionService.cs
│   │   ├── RepositoryIngestionService.cs
│   │   ├── ISourceFileClassifier.cs
│   │   ├── SourceFileClassifier.cs
│   │   ├── ISourceFileMetadataService.cs
│   │   ├── SourceFileMetadataService.cs
│   │   ├── ICodeChunkingService.cs
│   │   ├── CodeChunkingService.cs
│   │   ├── IEmbeddingService.cs
│   │   └── AzureOpenAIEmbeddingService.cs
│   ├── Retrieval/                       # Code context retrieval
│   │   ├── ICodeRetrievalService.cs
│   │   ├── CodeRetrievalService.cs
│   │   ├── ICodeContextBuilder.cs
│   │   ├── CodeContextBuilder.cs
│   │   ├── IQueryEmbeddingService.cs
│   │   ├── QueryEmbeddingService.cs
│   │   ├── IRetrievalResultProcessor.cs
│   │   └── RetrievalResultProcessor.cs
│   ├── Search/                          # Azure AI Search integration
│   │   ├── IHybridSearchService.cs
│   │   ├── HybridSearchService.cs
│   │   ├── IVectorSearchService.cs
│   │   ├── VectorSearchService.cs
│   │   ├── ISearchIndexDefinitionService.cs
│   │   ├── SearchIndexDefinitionService.cs
│   │   ├── ISearchIndexManagementService.cs
│   │   ├── SearchIndexManagementService.cs
│   │   ├── ISearchDocumentIndexingService.cs
│   │   └── SearchDocumentIndexingService.cs
│   ├── Tools/                           # Copilot tool extension framework
│   │   ├── ICopilotTool.cs
│   │   ├── ICopilotToolRegistry.cs
│   │   ├── CopilotToolRegistry.cs
│   │   ├── CodeSearchTool.cs
│   │   └── FileInspectionTool.cs
│   ├── ArchitectureAnalysis/            # Architecture analysis features
│   ├── CodeReview/                      # Code review features
│   ├── FileInspection/                  # File inspection features
│   └── TestGeneration/                  # Test generation features
├── Models/                              # Data transfer objects (DTOs)
│   ├── Copilot/                         # Copilot request/response models
│   ├── Repository/                      # Repository models
│   ├── Retrieval/                       # Retrieval models
│   ├── Search/                          # Search result models
│   ├── CodeReview/                      # Code review models
│   ├── ArchitectureAnalysis/            # Architecture analysis models
│   ├── FileInspection/                  # File inspection models
│   └── TestGeneration/                  # Test generation models
└── Configuration/                       # Strongly-typed configuration
    ├── AzureSearchOptions.cs
    ├── RetrievalOptions.cs
    └── RetrievalContextOptions.cs
```

## Target Framework

- **.NET 8.0** (Long-term support, performance-optimized)
- C# 12.0
- Nullable reference types enabled
- Implicit usings enabled

All code should leverage modern C# features and .NET 8 capabilities.

## API Endpoint Conventions

1. **Route Naming**
   - Use lowercase, plural nouns: `/api/copilot`, `/api/retrieval`, `/api/code-review`
   - Verb-based operations: `/api/copilot/ask`, `/api/retrieval/search`
   - Avoid arbitrary route structures; keep consistent with ASP.NET Core conventions

2. **Minimal APIs Pattern**
   - Register endpoints via extension methods in `Endpoints/` directory
   - Call `.MapXxxEndpoints(endpoints)` in `Program.cs`
   - Example:
     ```csharp
     var builder = WebApplication.CreateBuilder(args);
     var app = builder.Build();
     app.MapCopilotEndpoints();
     app.MapRetrievalEndpoints();
     ```

3. **HTTP Methods**
   - POST for operations that create state or trigger computation
   - GET for retrieval only
   - Use appropriate status codes: 200 OK, 201 Created, 400 Bad Request, 404 Not Found, 500 Internal Server Error

4. **Request Validation**
   - Validate all inputs at the endpoint handler level before calling services
   - Return `Results.BadRequest(new CopilotErrorResponse(...))` for validation failures
   - Example:
     ```csharp
     if (string.IsNullOrWhiteSpace(request.Query))
     {
         return Results.BadRequest(
             new CopilotErrorResponse("Query cannot be empty."));
     }
     ```

5. **Error Responses**
   - Use `CopilotErrorResponse` model for consistent error format
   - Include a user-friendly message
   - Log the full error context before returning

## Domain Models and Service Abstractions

1. **Interface-First Design**
   - Define interfaces before implementations: `IXxxService.cs`
   - Keep interfaces focused and single-responsibility
   - Example: `IAzureOpenAIService` for AI operations only

2. **Naming Conventions for Interfaces and Implementations**
   - Interface: `ICodeChunkingService`
   - Main implementation: `CodeChunkingService`
   - Alternative implementations: `AzureOpenAICodeChunkingService`, `FakeCodeChunkingService` (for tests)

3. **Models and DTOs**
   - Request models: `XxxRequest` (e.g., `CopilotRequest`)
   - Response models: `XxxResponse` (e.g., `CopilotResponse`)
   - Domain models: `Xxx` (e.g., `CodeChunk`)
   - Metadata models: `XxxMetadata` (e.g., `SourceFileMetadata`)

4. **Service Organization by Feature**
   - Group related services by feature in subdirectories
   - Example: `Services/Copilot/`, `Services/Repository/`, `Services/Retrieval/`
   - Each feature directory contains interfaces and implementations

## Dependency Injection

1. **Registration in Program.cs**
   - Register all services in `Program.cs` before building the app
   - Use appropriate lifetimes:
     - `AddSingleton<T>()` for stateless services (e.g., `SourceFileClassifier`)
     - `AddScoped<T>()` for request-scoped services (e.g., `CodeRetrievalService`)
     - `AddTransient<T>()` rarely used; avoid unless necessary

2. **Example Service Registration**
   ```csharp
   builder.Services.AddSingleton<IAzureOpenAIService, AzureOpenAIService>();
   builder.Services.AddScoped<ICodeRetrievalService, CodeRetrievalService>();
   builder.Services.AddScoped<ICodeContextBuilder, CodeContextBuilder>();
   ```

3. **HttpClient Factory**
   - Use `AddHttpClient<IXxx, Xxx>()` for services that make HTTP calls
   - Example: `AddHttpClient<IRepositoryProvider, GitHubRepositoryProvider>()`
   - Configure user agent and default headers in the factory

4. **Constructor Injection**
   - All dependencies must be constructor-injected
   - Mark dependencies as `readonly`
   - Validate non-null dependencies in constructor with `ArgumentNullException.ThrowIfNull()`
   - Example:
     ```csharp
     public sealed class CodeRetrievalService : ICodeRetrievalService
     {
         private readonly ICodeContextBuilder _contextBuilder;
         private readonly IAzureOpenAIService _aiService;

         public CodeRetrievalService(
             ICodeContextBuilder contextBuilder,
             IAzureOpenAIService aiService)
         {
             ArgumentNullException.ThrowIfNull(contextBuilder);
             ArgumentNullException.ThrowIfNull(aiService);

             _contextBuilder = contextBuilder;
             _aiService = aiService;
         }
     }
     ```

## Async/Await and Cancellation

1. **Async All The Way**
   - Any method performing I/O must be `async Task` or `async Task<T>`
   - Never use `.Result` or `.Wait()`

2. **CancellationToken Parameter**
   - All async methods must accept a `CancellationToken` parameter
   - Default value: `CancellationToken.None` or `cancellationToken = default`
   - Propagate cancellation token through the call chain
   - Call `cancellationToken.ThrowIfCancellationRequested()` at appropriate checkpoints
   - Example:
     ```csharp
     public async Task<CopilotResponse> AskAsync(
         CopilotRequest request,
         CancellationToken cancellationToken = default)
     {
         cancellationToken.ThrowIfCancellationRequested();
         
         var result = await _aiService.ChatAsync(
             request.Query,
             cancellationToken);
         
         return new CopilotResponse { Content = result };
     }
     ```

3. **Exception Handling for Cancellation**
   - Catch `OperationCanceledException` at endpoint level
   - Return HTTP 499 (Client Closed Request) when the client cancels
   - Example:
     ```csharp
     catch (OperationCanceledException) 
         when (cancellationToken.IsCancellationRequested)
     {
         return Results.StatusCode(StatusCodes.Status499ClientClosedRequest);
     }
     ```

## Validation and Error Handling

1. **Input Validation**
   - Validate at API endpoint before calling services
   - Use clear, specific error messages
   - For complex validation, delegate to service methods that throw `ArgumentException` or `ArgumentOutOfRangeException`

2. **Exception Types**
   - `ArgumentException`: Invalid argument value
   - `ArgumentNullException`: Null argument when non-null required
   - `ArgumentOutOfRangeException`: Value outside valid range
   - `InvalidOperationException`: Operation cannot be performed in current state
   - `OperationCanceledException`: Operation was cancelled
   - Custom exceptions: Only for domain-specific errors with clear semantics

3. **Error Logging**
   - Log errors with full context before returning HTTP error responses
   - Use `ILoggerFactory.CreateLogger("ComponentName")` to get a logger
   - Example:
     ```csharp
     catch (HttpRequestException ex)
     {
         logger.LogError(ex, "Failed to fetch code context: {Message}", ex.Message);
         return Results.StatusCode(500);
     }
     ```

## Configuration

1. **Strongly-Typed Configuration Options**
   - Create configuration classes in `Configuration/` directory
   - Use `[OptionsClass]` pattern from `Microsoft.Extensions.Options`
   - Bind from configuration in `Program.cs` via `AddOptions<T>().Bind()`
   - Example:
     ```csharp
     builder.Services
         .AddOptions<AzureSearchOptions>()
         .Bind(builder.Configuration.GetSection(AzureSearchOptions.SectionName));
     ```

2. **Configuration Class Example**
   ```csharp
   public class AzureSearchOptions
   {
       public const string SectionName = "AzureSearch";
       
       public string? Endpoint { get; set; }
       public string? ApiKey { get; set; }
       public string? IndexName { get; set; }
   }
   ```

3. **Never Hardcode Configuration**
   - Use `appsettings.json` for default values
   - Use `appsettings.{Environment}.json` for environment-specific overrides
   - Use .NET User Secrets for sensitive local development values
   - Use Azure Key Vault for production secrets (via managed identity)

## Azure OpenAI Integration

1. **Using IAzureOpenAIService**
   - All Azure OpenAI calls must go through `IAzureOpenAIService`
   - Do not instantiate Azure OpenAI client directly in services
   - Example:
     ```csharp
     public class CodeReviewService
     {
         public async Task<string> ReviewAsync(string code, CancellationToken cancellationToken)
         {
             var response = await _aiService.ChatAsync(
                 $"Review this code:\n{code}",
                 cancellationToken);
             return response;
         }
     }
     ```

2. **Streaming Responses**
   - Use the streaming endpoint for long-running operations
   - Leverage `IAsyncEnumerable<T>` for streaming data
   - Update frontend to consume streamed responses via Server-Sent Events (SSE)

3. **Rate Limiting**
   - Azure OpenAI has rate limits; implement retry logic with exponential backoff
   - Catch rate-limit exceptions and handle gracefully
   - Document rate-limit strategy in service implementation

4. **Model and Deployment Configuration**
   - Model/deployment name should be configurable (via options pattern)
   - Never hardcode deployment names or models
   - Support multiple models/deployments for fallback scenarios (future enhancement)

## Retrieval and Search

1. **Using ICodeRetrievalService**
   - For fetching relevant code context: use `ICodeRetrievalService`
   - Specify number of results via `Top` parameter
   - Example:
     ```csharp
     var codeContext = await _retrievalService.RetrieveAsync(
         query: "How does authentication work?",
         repository: "owner/repo",
         branch: "main",
         top: 5,
         cancellationToken);
     ```

2. **Using IHybridSearchService**
   - For combined vector + keyword search: use `IHybridSearchService`
   - Useful for repository-wide searches combining semantic and keyword relevance

3. **Context Building**
   - Use `ICodeContextBuilder` to construct the AI prompt context
   - Process results through `IRetrievalResultProcessor` before using
   - Include metadata (file path, line numbers) in context for traceability

## Tool Calling Framework

1. **Implementing a New Tool**
   - Implement `ICopilotTool` interface
   - Place in `Services/Tools/` directory
   - Example:
     ```csharp
     public class MyTool : ICopilotTool
     {
         public string Name => "my-tool";
         public string Description => "Describes what the tool does";
         
         public async Task<ToolResult> ExecuteAsync(
             ToolInput input,
             CancellationToken cancellationToken)
         {
             // Implementation
             return new ToolResult { /* ... */ };
         }
     }
     ```

2. **Registering a Tool**
   - Register the service in `Program.cs`
   - Register the tool interface in registry:
     ```csharp
     builder.Services.AddScoped<MyTool>();
     builder.Services.AddScoped<ICopilotTool>(sp => sp.GetRequiredService<MyTool>());
     ```

3. **Using Tools in Copilot**
   - The `CodeCopilotService` checks for `IToolCallingCodeAnswerGenerationService`
   - If available, it uses tool calling for answer generation
   - If not, falls back to retrieval-augmented generation

## Repository Access

1. **Using IRepositoryProvider**
   - For GitHub repository access: use `IRepositoryProvider` (GitHub implementation)
   - Do not hardcode GitHub API URLs or tokens in code
   - Example:
     ```csharp
     var files = await _provider.GetFilesAsync(
         owner: "owner",
         repo: "repo",
         branch: "main",
         cancellationToken);
     ```

2. **GitHub Authentication**
   - Token should be configured via .NET User Secrets (local) or environment (production)
   - Implementation details in `GitHubRepositoryProvider`
   - Document required configuration in `docs/runbooks/local-development.md`

3. **Rate Limiting**
   - GitHub API has rate limits (60 unauthenticated, 5000 authenticated per hour)
   - Implement caching to avoid repeated requests
   - Log rate-limit warnings

## File Operations and Workspace Safety

When implementing workspace or patch features:

1. **Path Validation**
   - Validate all file paths to prevent path traversal attacks
   - Use `Path.GetFullPath()` and verify the result is within the workspace
   - Reject paths containing `..` or absolute paths outside the workspace

2. **Safe File I/O**
   - Use `File.ReadAllTextAsync()`, `File.WriteAllTextAsync()` with proper error handling
   - Implement backup/rollback for destructive operations
   - Never execute code from untrusted sources

3. **Workspace Isolation**
   - Each workspace operation should be isolated from others
   - Use temporary directories for scratch work
   - Clean up resources (files, directories) after operations complete

## Testing

1. **Test Project Structure**
   - Tests live in `tests/Copilot.Api.Tests/`
   - Namespace mirrors backend: `Copilot.Api.Tests.Services.Copilot` mirrors `Copilot.Api.Services.Copilot`
   - Test files: `XxxServiceTests.cs`, `XxxEndpointTests.cs`, etc.

2. **Unit Test Example**
   ```csharp
   [Fact]
   public async Task AskAsync_WithValidRequest_ReturnsResponse()
   {
       // Arrange
       var mockRetrieval = new Mock<ICodeRetrievalService>();
       mockRetrieval
           .Setup(x => x.RetrieveAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
           .ReturnsAsync(new CodeRetrievalResponse { /* ... */ });
       
       var service = new CodeCopilotService(mockRetrieval.Object, /* ... */);
       var request = new CopilotRequest { Query = "test", Repository = "owner/repo", Branch = "main", Top = 5 };
       
       // Act
       var response = await service.AskAsync(request);
       
       // Assert
       Assert.NotNull(response);
   }
   ```

3. **Integration Test Example (with ApiFactory)**
   ```csharp
   [Fact]
   public async Task CopilotEndpoint_WithValidRequest_ReturnsOk()
   {
       // Arrange
       using var client = new ApiFactory().CreateClient();
       var request = new CopilotRequest { /* ... */ };
       
       // Act
       var response = await client.PostAsJsonAsync("/api/copilot/ask", request);
       
       // Assert
       Assert.Equal(HttpStatusCode.OK, response.StatusCode);
   }
   ```

4. **Fake Implementations for Testing**
   - Create fake implementations in test directory: `FakeXxxService.cs`
   - Use for integration tests that don't require real Azure services
   - Examples: `FakeAzureOpenAIService`, `FakeCodeRetrievalService`

5. **Running Tests**
   - Command: `dotnet test`
   - Specific test file: `dotnet test tests/Copilot.Api.Tests/Services/Copilot/CodeCopilotServiceTests.cs`
   - Specific test: `dotnet test --filter "FullyQualifiedName~CodeCopilotServiceTests.AskAsync_WithValidRequest_ReturnsResponse"`

## Code Style and Standards

See [Coding Standards](../../docs/development/coding-standards.md) for detailed C# conventions.

**Quick Reference:**
- Use nullable reference types (`#nullable enable` at file top if needed)
- Use `sealed` for classes not designed for inheritance
- Use `readonly` for immutable fields
- Use `record` for value objects (request/response models)
- Use `enum` for fixed sets of values
- XML documentation comments for public APIs
- Avoid abbreviations; prefer clear names

## Useful Commands

```bash
# Build
dotnet build
dotnet build -c Release

# Test
dotnet test
dotnet test --configuration Release

# Clean
dotnet clean

# Run
dotnet run --project src/backend/Copilot.Api

# User Secrets
dotnet user-secrets init --project src/backend/Copilot.Api
dotnet user-secrets set "AzureOpenAI:Endpoint" "<endpoint>" --project src/backend/Copilot.Api
dotnet user-secrets list --project src/backend/Copilot.Api
```

## Links

- [Global Instructions](../copilot-instructions.md)
- [Architecture Overview](../../docs/architecture/architecture-overview.md)
- [Component Design](../../docs/architecture/component-design.md)
- [Coding Standards](../../docs/development/coding-standards.md)
- [Testing Strategy](../../docs/development/testing-strategy.md)
- [Local Development Runbook](../../docs/runbooks/local-development.md)
