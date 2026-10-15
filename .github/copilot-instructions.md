# GitHub Copilot Instructions for AI Software Engineer Copilot

This file provides repository-wide guidance for GitHub Copilot assisting with development in the AI Software Engineer Copilot project.

## Product Purpose

The **AI Software Engineer Copilot** is an extensible, production-oriented AI platform designed to assist software developers with:

- AI-powered code understanding and navigation
- Repository intelligence and search
- Code review and analysis
- Architecture analysis
- Automated test generation
- Developer workflow integration

The platform consists of:

- **Backend:** ASP.NET Core .NET 8 API with Azure OpenAI, Azure AI Search, and GitHub integrations
- **Frontend:** React 19 TypeScript UI with streaming response support
- **Testing:** Comprehensive xUnit test suite with dependency injection and mocking

The system is designed to be extensible, maintainable, and production-ready.

## Before Starting Implementation

1. **Read Authoritative Documentation**
   - [Architecture Overview](../../docs/architecture/architecture-overview.md)
   - [Component Design](../../docs/architecture/component-design.md)
   - [Implementation Status](../../docs/development/implementation-status.md)
   - [Coding Standards](../../docs/development/coding-standards.md)

2. **Inspect Related Code**
   - Use your IDE's symbol search to understand existing patterns
   - Review test files for examples of correct usage
   - Do not assume; verify that classes, endpoints, and abstractions actually exist

3. **Understand Architectural Layering**
   - API Endpoints → Services → Dependencies
   - Use dependency injection for all external resources
   - Keep service boundaries clear; avoid circular dependencies

4. **Check for Existing Implementations**
   - Search for existing services, interfaces, and models before creating new ones
   - Reuse existing abstractions wherever possible
   - Link to existing implementation in architecture documents

## Security and Secrets

- **Never commit secrets, API keys, connection strings, or credentials**
- Use .NET User Secrets for local development only
- Production configuration uses Azure Key Vault and managed identity
- All sensitive values in examples must be clearly marked as `<PLACEHOLDER>` or `[REDACTED]`
- When documenting configuration, use placeholder names only; never include actual values

## Architecture and Design Rules

1. **Dependency Injection**
   - Register all external dependencies in `Program.cs`
   - Use interfaces for all services (e.g., `IAzureOpenAIService`)
   - Prefer constructor injection
   - Use appropriate lifetimes: Singleton for stateless services, Scoped for request-specific

2. **Async/Await and Cancellation**
   - All I/O operations must be async
   - Always accept and respect `CancellationToken` in async methods
   - Use `cancellationToken.ThrowIfCancellationRequested()` where appropriate
   - Do not use `.Result` or `.Wait()`

3. **Validation and Error Handling**
   - Validate inputs at API endpoints and service boundaries
   - Use specific exception types (e.g., `ArgumentException`, `InvalidOperationException`)
   - Return meaningful error responses with `CopilotErrorResponse`
   - Log errors with context before returning HTTP error responses

4. **Configuration**
   - Use strongly-typed configuration options classes in `Configuration/` directory
   - Bind from `appsettings.json` and `.netconfig` via Options pattern
   - Environment-specific configuration via `appsettings.{Environment}.json`
   - Never embed configuration values in code

5. **Azure OpenAI Integration**
   - Use `IAzureOpenAIService` for all AI interactions
   - Respect rate limits and handle rate-limit exceptions
   - Implement streaming where appropriate for better UX
   - Include model/deployment name as configurable option

6. **Retrieval and Search**
   - Use `ICodeRetrievalService` for fetching relevant code context
   - Use `IHybridSearchService` for combined vector + keyword search
   - Process results through `IRetrievalResultProcessor`
   - Build context via `ICodeContextBuilder` before passing to AI

7. **Tool Calling**
   - Extend via `ICopilotTool` interface
   - Register tools in `ICopilotToolRegistry`
   - Keep tools focused and single-responsibility
   - Return structured results for composition

8. **Repository Access**
   - Use `IRepositoryProvider` (GitHub implementation) for repository operations
   - Handle authentication via configured credentials (not exposed in code)
   - Implement rate-limit awareness for GitHub API
   - Cache repository metadata where appropriate

9. **File Operations**
   - When implementing workspace or patch features: validate all file paths
   - Prevent path traversal attacks
   - Never execute arbitrary code from untrusted sources
   - Use safe file I/O APIs with proper error handling

10. **Prompt and Context Injection**
    - Use `CopilotPromptBuilder` for consistent prompt construction
    - Be aware of prompt injection risks from untrusted repository content
    - Sanitize or escape user input before including in prompts
    - Document assumptions about input boundaries

## Testing and Verification Expectations

1. **Unit Tests**
   - Write tests for new services, using dependency injection and Moq for mocks
   - Place tests in parallel namespace under `tests/Copilot.Api.Tests/`
   - Use meaningful test names that describe the scenario and expected outcome
   - Mock external dependencies (Azure OpenAI, GitHub, Azure AI Search)

2. **Integration Tests**
   - Use `ApiFactory` test factory for API endpoint testing
   - Test complete request→response flow without real external services
   - Use fake implementations for Azure OpenAI and other services
   - Example: `FakeAzureOpenAIService`, `FailingAzureOpenAIService` for failure scenarios

3. **Verification Before Committing**
   - Run `dotnet test` to ensure all tests pass
   - Run `dotnet build -c Release` to verify release build
   - Use the relevant `.instructions.md` file for feature-specific verification steps
   - Confirm no new secrets were added

## Documentation Synchronization

1. **When You Implement a Feature**
   - Update `docs/development/implementation-status.md` with implementation status
   - Link your implementation to relevant architecture documents
   - Add any new configuration keys to `docs/runbooks/local-development.md`
   - Create or update tests; document test strategy in `docs/development/testing-strategy.md`

2. **When You Change Architecture**
   - Create or update an Architecture Decision Record (ADR) in `docs/architecture/adr/`
   - Explain the decision, alternatives considered, and consequences
   - Update `docs/architecture/component-design.md` if component interfaces change
   - Update `docs/architecture/data-flow.md` if data flows change

3. **When You Change a Requirement**
   - Update `docs/product/feature-roadmap.md` or `docs/product/product-requirements.md`
   - Clearly mark which work is planned, in-progress, blocked, or complete
   - Include actual evidence: links to tests, source paths, commit references

4. **Conflicting Documentation**
   - Do not silently choose which version is correct
   - Explicitly document the discrepancy in the relevant file
   - Mark the issue as requiring verification or decision
   - Update documentation as soon as the conflict is resolved

## Git Workflow and Approval

1. **Branching**
   - Create feature branches from `develop` or `main`
   - Use descriptive branch names: `feature/xyz`, `fix/abc`, `docs/overview`

2. **Commits**
   - Write clear, descriptive commit messages
   - Reference relevant issues or ADRs when applicable
   - Keep commits focused; avoid mixing unrelated changes

3. **User Approval**
   - **Always get user approval before:**
     - Making architectural changes affecting multiple components
     - Modifying Azure service integrations
     - Adding new external dependencies
     - Creating or updating Copilot instructions
     - Committing documentation changes that affect future guidance
   - Use pull requests to propose changes; do not force-push or bypass review

4. **No Blind Commits or Pushes**
   - Do not commit or push without explicit user authorization
   - Always show diffs and implementation summaries first
   - Report any risks, security concerns, or breaking changes

## Features and Implementation Status

### Implemented
- Azure OpenAI integration (chat, streaming, embeddings)
- Repository ingestion and metadata extraction
- Code chunking and embedding
- Azure AI Search indexing and retrieval
- Copilot service orchestration
- Tool calling framework with CodeSearch and FileInspection tools
- Architecture analysis service
- Code review service
- Test generation service
- React frontend with streaming support
- Comprehensive test suite
- CI/CD via GitHub Actions

### Partially Implemented
- Repository workspace management (structure exists, endpoints not fully tested)

### Planned
- Persistent conversation storage
- Workspace patch generation and validation
- Azure DevOps integration
- Advanced observability and diagnostics
- Performance optimization

### Not Implemented / Unknown
- Formal security audit
- Deployment to production Azure environment
- Performance benchmarking
- Compliance validation

## Handling Uncertainty

1. **If a feature status is unclear:** Mark it as "Not verified" and research the code
2. **If documentation conflicts with implementation:** Document the conflict and ask for clarification
3. **If you cannot find an abstraction you need:** Propose a new service interface in `Services/` directory following existing patterns
4. **If a requirement is ambiguous:** Ask for specific acceptance criteria before implementing

## When to Ask for Guidance

- Before making architectural changes
- Before adding external dependencies
- Before committing security-sensitive code
- Before declaring a component complete
- When implementation differs significantly from design

## Useful Repository Locations

- **Backend API code:** `src/backend/Copilot.Api/`
  - Endpoints: `src/backend/Copilot.Api/Endpoints/`
  - Services: `src/backend/Copilot.Api/Services/`
  - Models: `src/backend/Copilot.Api/Models/`
  - Configuration: `src/backend/Copilot.Api/Configuration/`

- **Frontend code:** `src/frontend/src/`
  - Components, pages, utilities

- **Tests:** `tests/Copilot.Api.Tests/`
  - Organized by feature area (Copilot/, Retrieval/, Search/, etc.)

- **Architecture documentation:** `docs/architecture/`
  - Overview, system context, components, data flows, security, ADRs

- **Product documentation:** `docs/product/`
  - Requirements, roadmap, MVP definition

- **Development documentation:** `docs/development/`
  - Implementation status, sprint history, coding standards, testing strategy

- **Runbooks:** `docs/runbooks/`
  - Local development setup, troubleshooting

- **CI/CD:** `.github/workflows/ci.yml`

- **Copilot Configuration:** `.github/` directory
  - `copilot-instructions.md` (this file)
  - `instructions/` - Feature-specific instructions
  - `prompts/` - Reusable prompt templates

## For More Information

- See [Architecture Overview](../../docs/architecture/architecture-overview.md) for system architecture
- See [Coding Standards](../../docs/development/coding-standards.md) for language-specific conventions
- See [Testing Strategy](../../docs/development/testing-strategy.md) for test patterns and commands
- See [Local Development](../../docs/runbooks/local-development.md) for setup instructions
- See individual `.instructions.md` files in `.github/instructions/` for feature-specific guidance
