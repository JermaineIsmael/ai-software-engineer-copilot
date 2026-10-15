# Coding Standards

This document defines the C# and .NET coding conventions and best practices for the AI Software Engineer Copilot repository.

## Language: C#

**Target Version:** C# 12.0 (with .NET 8)  
**Nullable Reference Types:** Enabled (required)  
**Implicit Usings:** Enabled (required)

## Naming Conventions

### Namespaces
- Pattern: `Copilot.Api.[Feature]`
- Examples: `Copilot.Api.Services.Copilot`, `Copilot.Api.Models.Retrieval`
- PascalCase, descriptive names

### Classes and Structs
- PascalCase
- Descriptive nouns
- Examples: `AuthService`, `CopilotResponse`, `CodeChunk`
- No prefix/suffix for class type (avoid I prefix for non-interfaces)

### Interfaces
- PascalCase with leading `I`
- Examples: `IAzureOpenAIService`, `ICodeRetrievalService`
- One word or short phrase preferred

### Methods
- PascalCase
- Verb or verb-phrase
- Async methods: suffix with `Async`
- Examples: `GetAsync()`, `RetrieveAsync()`, `ChatAsync()`

### Properties
- PascalCase
- Noun or noun-phrase
- Examples: `Content`, `FilePath`, `LineNumbers`

### Local Variables and Parameters
- camelCase
- Descriptive names (avoid single letters except loop variables)
- Examples: `codeContext`, `queryVector`, `repositoryName`
- Loop variables: `i`, `j` acceptable for `for` loops
- Loop variables: Use descriptive for `foreach` loops

### Constants
- PascalCase (treated as properties)
- `const` modifier preferred for compile-time constants
- Example: `const string SectionName = "AzureSearch"`

### Private Fields
- camelCase with leading underscore
- Example: `_retrievalService`, `_contextBuilder`
- `readonly` modifier required for all fields

## Code Organization

### File Structure

```csharp
// 1. Using statements (sorted alphabetically, system first)
using System;
using System.Collections.Generic;
using Copilot.Api.Models;
using Copilot.Api.Services;

// 2. Namespace
namespace Copilot.Api.Services.Copilot;

// 3. Class/interface declaration (one per file recommended)
public sealed class CodeCopilotService : ICodeCopilotService
{
    // 4. Private fields
    private readonly ICodeRetrievalService _retrievalService;
    private readonly ICodeContextBuilder _contextBuilder;

    // 5. Constructor(s)
    public CodeCopilotService(
        ICodeRetrievalService retrievalService,
        ICodeContextBuilder contextBuilder)
    {
        ArgumentNullException.ThrowIfNull(retrievalService);
        ArgumentNullException.ThrowIfNull(contextBuilder);
        
        _retrievalService = retrievalService;
        _contextBuilder = contextBuilder;
    }

    // 6. Public methods
    public async Task<CopilotResponse> AskAsync(
        CopilotRequest request,
        CancellationToken cancellationToken = default)
    {
        // Implementation
    }

    // 7. Private methods
    private void ValidateRequest(CopilotRequest request)
    {
        // Implementation
    }
}
```

### Access Modifiers
- Default to `private`; make `public` only when necessary
- Use `sealed` for classes not designed for inheritance
- Use `internal` for types only needed within assembly

## Declaration Patterns

### Type Declarations

```csharp
// Record for immutable DTOs
public record CopilotRequest(
    string Query,
    string Repository,
    string Branch,
    int Top);

// Class for mutable services
public sealed class CodeRetrievalService : ICodeRetrievalService
{
    // Implementation
}

// Interface for contracts
public interface ICodeRetrievalService
{
    Task<CodeRetrievalResponse> RetrieveAsync(
        string query,
        string repository,
        string branch,
        int top,
        CancellationToken cancellationToken);
}
```

### Method Signatures

```csharp
// Parameters on same line if short
public async Task<string> GetAsync(string id, CancellationToken cancellationToken)

// Parameters on separate lines if long
public async Task<CopilotResponse> AskAsync(
    CopilotRequest request,
    CancellationToken cancellationToken = default)

// Always include CancellationToken for async methods
public async Task ProcessAsync(
    string input,
    CancellationToken cancellationToken = default)
```

## Modern C# Features

### Nullable Reference Types
- Enable with `#nullable enable` (or project-wide in .csproj)
- Prefix type with `?` for nullable: `string?`, `List<T>?`
- Validate non-nullable parameters: `ArgumentNullException.ThrowIfNull(param)`

### Records
- Use for immutable DTOs and models
- Example:
  ```csharp
  public record CodeChunk(
      string Content,
      string FilePath,
      int[] LineNumbers,
      float Score);
  ```

### Async/Await
- All I/O methods must be `async Task` or `async Task<T>`
- No `.Result` or `.Wait()`
- Always propagate `CancellationToken`

### Using Declarations
- Use `using` declarations (top of method) for resource cleanup
- Example:
  ```csharp
  using var response = await httpClient.GetAsync(url);
  ```

### Pattern Matching
- Use `is` patterns for type checking
- Example:
  ```csharp
  if (response is { StatusCode: 200 })
  {
      // Handle success
  }
  ```

### Target-Typed New
- Use `new()` when type is clear from context
- Example:
  ```csharp
  List<CodeChunk> chunks = new();  // Type inferred
  ```

## Error Handling

### Exception Types
- `ArgumentException`: Invalid argument value
- `ArgumentNullException`: Null when non-null required
- `ArgumentOutOfRangeException`: Value outside valid range
- `InvalidOperationException`: Operation cannot be performed in current state
- `OperationCanceledException`: Operation was cancelled

### Validation Pattern
```csharp
public CodeRetrievalService(
    ICodeRetrievalService retrievalService,
    ICodeContextBuilder contextBuilder)
{
    // Validate all dependencies
    ArgumentNullException.ThrowIfNull(retrievalService);
    ArgumentNullException.ThrowIfNull(contextBuilder);
    
    _retrievalService = retrievalService;
    _contextBuilder = contextBuilder;
}
```

### Exception Handling
```csharp
try
{
    var result = await _aiService.ChatAsync(prompt, cancellationToken);
    return result;
}
catch (OperationCanceledException)
    when (cancellationToken.IsCancellationRequested)
{
    throw;  // Re-throw; let caller handle
}
catch (HttpRequestException ex)
{
    _logger.LogError(ex, "Failed to call AI service: {Message}", ex.Message);
    throw new InvalidOperationException("AI service unavailable", ex);
}
```

## Async/Await and Cancellation

```csharp
// Always accept CancellationToken
public async Task<T> GetAsync(
    string id,
    CancellationToken cancellationToken = default)
{
    cancellationToken.ThrowIfCancellationRequested();
    
    var result = await _service.FetchAsync(id, cancellationToken);
    
    cancellationToken.ThrowIfCancellationRequested();
    
    return result;
}
```

## Logging

```csharp
// Use ILoggerFactory or dependency-injected ILogger
var logger = loggerFactory.CreateLogger("FeatureName");

// Log errors with context
catch (Exception ex)
{
    logger.LogError(ex, "Operation failed for {Id}: {Message}", id, ex.Message);
}

// No sensitive data in logs
logger.LogInformation("User query: {Query}", query);  // OK
logger.LogInformation("API key: {Key}", apiKey);      // NEVER
```

## Comments and Documentation

### XML Documentation (Public APIs)
```csharp
/// <summary>
/// Retrieves code context for a given query.
/// </summary>
/// <param name="query">The search query.</param>
/// <param name="repository">Repository owner/name.</param>
/// <param name="branch">Branch name.</param>
/// <param name="top">Number of results to return.</param>
/// <param name="cancellationToken">Cancellation token.</param>
/// <returns>Code retrieval response with matched chunks.</returns>
/// <exception cref="ArgumentException">If query is empty or top <= 0.</exception>
public async Task<CodeRetrievalResponse> RetrieveAsync(
    string query,
    string repository,
    string branch,
    int top,
    CancellationToken cancellationToken = default)
```

### Inline Comments
- Only for non-obvious logic
- Keep brief and explain WHY, not WHAT
- Good: `// Retry on transient failures (429, 5xx)`
- Bad: `// Set count to 5` (obvious from code)

## Formatting

### Indentation
- 4 spaces (no tabs)

### Brace Style
- Allman style (brace on next line)
  ```csharp
  if (condition)
  {
      // Code
  }
  ```

### Line Length
- Target: 100 characters
- Acceptable up to 120 characters
- Break long lines logically

### Whitespace
- One blank line between methods
- No trailing whitespace
- Space after keywords: `if (`, `for (`, `foreach (`
- Spaces around binary operators: `a + b`, `x = 5`

## Testing Patterns

### Test Naming
```csharp
[Fact]
public async Task AskAsync_WithValidRequest_ReturnsResponse()
{
    // Arrange
    var request = new CopilotRequest { /* ... */ };
    var mockService = new Mock<ICodeRetrievalService>();
    // ...
    
    // Act
    var response = await service.AskAsync(request);
    
    // Assert
    Assert.NotNull(response);
}
```

### Mocking Pattern
```csharp
var mockService = new Mock<ICodeRetrievalService>();
mockService
    .Setup(x => x.RetrieveAsync(
        It.IsAny<string>(),
        It.IsAny<string>(),
        It.IsAny<string>(),
        It.IsAny<int>(),
        It.IsAny<CancellationToken>()))
    .ReturnsAsync(expectedResponse);
```

## Git and Version Control

### Commit Messages
- First line: 50 characters max, present tense
- Example: `Add code retrieval service tests`
- Blank line, then detailed explanation
- Reference issues: `Fixes #123`

### Branch Naming
- Feature: `feature/description`
- Fix: `fix/description`
- Docs: `docs/description`
- Example: `feature/hybrid-search-implementation`

## Tools and Enforcement

### EditorConfig
Project includes `.editorconfig` for automatic enforcement of:
- Indentation (4 spaces)
- Charset (UTF-8)
- Trim trailing whitespace

### Code Analysis
- Roslyn analyzers configured in project
- Warnings treated as errors in Release builds
- No suppression of warnings without justification

### Unit Testing
- xUnit framework
- Moq for mocking
- Test files mirror source structure
- Naming: `XxxServiceTests.cs`, `XxxEndpointTests.cs`

## Useful Commands

```bash
# Format code (if dotnet-format installed)
dotnet format

# Check for issues
dotnet build
dotnet test

# Analyze code (static analysis)
dotnet publish -c Release
```

## Related Documents

- [Global Instructions](../../.github/copilot-instructions.md)
- [Backend Instructions](../../.github/instructions/backend.instructions.md)
- [Testing Strategy](testing-strategy.md)
