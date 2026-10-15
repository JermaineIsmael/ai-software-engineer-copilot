# ADR 0001: Minimal APIs for ASP.NET Core Backend

**Date:** Not specified (inferred from implementation)  
**Status:** Accepted (currently implemented)  
**Context:** AI Software Engineer Copilot backend HTTP API design  

## Context

The AI Software Engineer Copilot requires an HTTP API to serve requests from a React frontend and potential future clients. The team needed to decide on the architectural pattern for implementing the API.

## Decision

Use **ASP.NET Core Minimal APIs** (introduced in .NET 6) as the primary pattern for implementing HTTP endpoints.

### Implementation

- All endpoints defined in `Endpoints/` directory using extension methods
- Each feature area has its own endpoint file (e.g., `CopilotEndpoints.cs`, `RetrievalEndpoints.cs`)
- Endpoints registered in `Program.cs` using `.MapXxxEndpoints()` pattern
- Direct dependency injection into route handlers

Example:
```csharp
public static class CopilotEndpoints
{
    public static IEndpointRouteBuilder MapCopilotEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/api/copilot/ask", HandleAskAsync);
        return endpoints;
    }

    private static async Task<IResult> HandleAskAsync(
        CopilotRequest request,
        ICodeCopilotService copilotService,
        ILoggerFactory loggerFactory,
        CancellationToken cancellationToken)
    {
        // Handler implementation
    }
}
```

## Alternatives Considered

1. **ASP.NET Core MVC/Controllers Pattern**
   - Pros: Well-established, more documentation, inheritance-based
   - Cons: More boilerplate, heavier framework overhead, less suitable for small/focused APIs
   - Rejected: Team prioritized simplicity and performance

2. **Microservices with multiple projects**
   - Pros: Finer separation of concerns, independent scaling
   - Cons: Added complexity, deployment complexity, operational overhead
   - Rejected: Monolithic approach is sufficient for current and planned scope

3. **GraphQL**
   - Pros: Flexible query language, single endpoint, precise data fetching
   - Cons: More complex, larger payload for simple queries, learning curve
   - Rejected: REST is simpler and sufficient for the use case

## Consequences

### Positive

1. **Minimal Boilerplate** - Endpoints are concise and focused
2. **Performance** - Lower overhead compared to MVC, suitable for cloud deployment
3. **Testability** - Can inject services directly; easy to create test implementations
4. **Maintainability** - Clear endpoint→service mapping; easy to locate and modify endpoints
5. **Modern .NET** - Leverages latest framework capabilities and patterns
6. **Dependency Injection** - Full DI support without additional frameworks

### Negative

1. **Unfamiliarity** - Developers familiar with MVC may need ramp-up time
2. **Scalability Considerations** - If API grows significantly (100+ endpoints), might benefit from more structured routing
3. **Conventions** - Requires discipline to maintain consistent patterns (e.g., error handling)
4. **IDE Support** - Less sophisticated tooling compared to MVC (attribute routing, scaffolding)

### Implementation Trade-offs

- All request validation happens in endpoint handlers (slightly duplicated logic)
  - Alternative: Middleware-based validation (rejected for clarity)
- Endpoint extension methods require `IEndpointRouteBuilder` parameter
  - Trade-off: Clear separation vs. less idiomatic

## Related Decisions

- [ADR 0002 (planned)](#): Dependency Injection Strategy
- [ADR 0003 (planned)](#): Service Layer Organization

## Supporting Evidence

### Code Inspection Artifacts

- **Endpoint Organization:** `src/backend/Copilot.Api/Endpoints/` directory
  - 6 endpoint files covering different feature areas
  - Consistent pattern across all files
  - Clean separation of concerns

- **Service Integration:** All endpoints correctly use dependency injection
  - Services injected into route handlers
  - Configuration options bound in `Program.cs`
  - Verified in tests: `ChatEndpointTests.cs`, etc.

- **Error Handling:** Endpoints validate input and return appropriate HTTP responses
  - Example: `CopilotEndpoints.cs` validates query, repository, branch, top parameters
  - Returns `Results.BadRequest()` for validation failures
  - Consistent error response format

### Test Coverage

- Integration tests in `tests/Copilot.Api.Tests/` use `ApiFactory`
- Tests successfully route requests to correct endpoint handlers
- Endpoint functionality verified via HTTP status codes and response content

## Lessons Learned

1. **Consistency is Critical:** All endpoints must follow the same validation and error handling patterns
2. **Documentation Required:** Developers must understand the pattern; consider adding endpoint documentation
3. **Scalability:** As endpoints grow (100+), consider introducing route groups or additional organization

## Future Considerations

- As API grows, monitor for patterns that could benefit from additional structure
- Consider API documentation (OpenAPI/Swagger) enhancements
- Evaluate performance metrics to ensure Minimal APIs remain suitable
- If workspace or complex features added, may reconsider separation into multiple projects

## Approval Notes

- Accepted as-is; no changes needed
- This decision remains current as of the documentation refresh
- Pattern has proven effective for existing endpoints
