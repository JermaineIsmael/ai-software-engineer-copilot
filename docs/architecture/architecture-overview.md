# Architecture Overview

## Product Purpose

The **AI Software Engineer Copilot** is a production-oriented, extensible AI platform designed to assist software developers with code understanding, repository intelligence, code review, architecture analysis, and developer workflows. The system leverages Azure OpenAI for AI reasoning, Azure AI Search for semantic code retrieval, and integrates with GitHub for repository access.

## High-Level Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                          DEVELOPER USERS                         │
│                    (Browser / IDE Integration)                   │
└──────────────────────────┬──────────────────────────────────────┘
                           │
                    HTTP / WebSocket
                           │
          ┌────────────────▼──────────────────┐
          │     React Frontend SPA            │
          │  (React 19, TypeScript, Vite)     │
          │  Streaming Response UI            │
          └────────────────┬──────────────────┘
                           │
                    HTTP REST / SSE
                           │
        ┌──────────────────▼──────────────────────┐
        │   ASP.NET Core 8 Minimal API            │
        │  (HTTP Endpoints, Request Validation)   │
        └──────────────────┬──────────────────────┘
                           │
              ┌────────────┴──────────────┐
              ▼                           ▼
    ┌─────────────────────┐    ┌──────────────────────┐
    │  Copilot Service    │    │   Other Services     │
    │  (Orchestration)    │    │ (Review, Analysis,   │
    └─────────────────────┘    │  Inspection, Test)   │
              │                └──────────────────────┘
              │                         │
    ┌─────────┴────────────┬───────────┘
    ▼                      ▼
┌──────────────────┐  ┌─────────────────────┐
│  Code Retrieval  │  │  Repository Access  │
│  Service         │  │  (GitHub API)       │
├──────────────────┤  ├─────────────────────┤
│• Query Embedding │  │• File retrieval     │
│• Context Builder │  │• Branch listing     │
│• Result Process  │  │• Metadata extraction│
└────────┬─────────┘  └──────────┬──────────┘
         │                       │
         ▼                       ▼
    ┌─────────────────┐   ┌──────────────┐
    │ Search Service  │   │  Repository  │
    │                 │   │  Ingestion   │
    │• Hybrid Search  │   │  Service     │
    │  (Vector +      │   │              │
    │   Keyword)      │   │• Chunking    │
    └────────┬────────┘   │• Embedding   │
             │            └──────┬───────┘
    ┌────────┴──────────┐        │
    ▼                   ▼        ▼
┌──────────────┐  ┌──────────────┐
│ Azure AI     │  │ Azure OpenAI │
│ Search       │  │              │
│              │  │• Chat Models │
│• Indexing    │  │• Embeddings  │
│• Retrieval   │  │• Streaming   │
└──────────────┘  └──────────────┘
```

## Core Components

### 1. API Layer (Endpoints)

**Location:** `src/backend/Copilot.Api/Endpoints/`

HTTP endpoint handlers using ASP.NET Core Minimal APIs:

- **CopilotEndpoints:** Main copilot query interface (`POST /api/copilot/ask`)
- **RetrievalEndpoints:** Code context retrieval (`POST /api/retrieval/*`)
- **CodeReviewEndpoints:** Code review features (`POST /api/code-review/*`)
- **ArchitectureAnalysisEndpoints:** Architecture analysis (`POST /api/architecture-analysis/*`)
- **FileInspectionEndpoints:** File inspection (`POST /api/file-inspection/*`)
- **TestGenerationEndpoints:** Test generation (`POST /api/test-generation/*`)

**Responsibilities:**
- Request validation
- Parameter parsing
- Response serialization
- HTTP status code assignment
- Error handling and logging

### 2. Service Layer

**Location:** `src/backend/Copilot.Api/Services/`

Business logic organized by feature area:

#### Core AI Service
- **AzureOpenAIService:** Abstracts Azure OpenAI API access
  - Chat completion (standard and streaming)
  - Embedding generation
  - Rate limit handling
  - Error recovery

#### Copilot Orchestration (`Services/Copilot/`)
- **CodeCopilotService:** Main orchestrator
  - Coordinates retrieval, context building, and answer generation
  - Supports tool-based answer generation (alternative path)
  - Validates input and manages workflows

- **CodeAnswerGenerationService:** RAG-based answer generation
  - Retrieves code context
  - Constructs prompts
  - Calls AI service
  - Formats responses

- **ToolCallingCodeAnswerGenerationService:** Tool-based answer generation
  - Supports function calling
  - Orchestrates tool execution
  - Aggregates tool results

#### Repository Access (`Services/Repository/`)
- **GitHubRepositoryProvider:** GitHub repository API client
- **RepositoryIngestionService:** Repository processing pipeline
- **SourceFileClassifier:** Language/type detection
- **SourceFileMetadataService:** Metadata extraction
- **CodeChunkingService:** Code segmentation
- **AzureOpenAIEmbeddingService:** Embedding generation for code chunks

#### Code Retrieval (`Services/Retrieval/`)
- **CodeRetrievalService:** Retrieval orchestrator
  - Coordinates search and context building
  - Formats results for AI consumption

- **QueryEmbeddingService:** Query embedding
- **CodeContextBuilder:** Constructs AI prompt context
- **RetrievalResultProcessor:** Post-processes search results

#### Search (`Services/Search/`)
- **HybridSearchService:** Combined vector + keyword search
- **VectorSearchService:** Semantic similarity search via embeddings
- **SearchIndexManagementService:** Index lifecycle
- **SearchDocumentIndexingService:** Document ingestion
- **SearchIndexDefinitionService:** Schema definition

#### Tool Framework (`Services/Tools/`)
- **CopilotToolRegistry:** Tool registration and discovery
- **ICopilotTool:** Interface for extensible tools
- **CodeSearchTool:** Code search tool
- **FileInspectionTool:** File inspection tool
- **WorkspaceCreateTool / ModifyCodeTool / DiffWorkspaceTool / WorkspaceCleanupTool:** Isolated repository snapshot modification workflow

#### Workspace Services (`Services/Workspace/`)
- **WorkspaceManager:** Creates, persists, leases, and cleans up isolated repository snapshots
- **PatchApplicationService:** Validates and applies bounded file operations with rollback
- **DiffService:** Reports changes against an immutable baseline
- **WorkspaceOperationLock:** Serializes operations per workspace

#### Additional Services
- **ArchitectureAnalysisService:** Architecture analysis
- **CodeReviewService:** Code review generation
- **TestGenerationService:** Test case generation
- **FileInspectionService:** File content analysis

### 3. Models and DTOs

**Location:** `src/backend/Copilot.Api/Models/`

Data transfer objects organized by feature:

- **Copilot Models:** `CopilotRequest`, `CopilotResponse`, `CopilotErrorResponse`, `CopilotSource`
- **Repository Models:** `RepositorySnapshot`, `RepositoryFile`, `CodeChunk`, `SourceFileMetadata`
- **Retrieval Models:** `CodeRetrievalRequest`, `CodeRetrievalResponse`, `RetrievedCodeChunk`
- **Search Models:** `CodeChunkSearchDocument`, `HybridSearchResult`
- **Feature Models:** Review findings, architecture components, test cases

### 4. Configuration

**Location:** `src/backend/Copilot.Api/Configuration/`

Strongly-typed options classes:

- **AzureSearchOptions:** Azure AI Search configuration
- **RetrievalOptions:** Code retrieval settings
- **RetrievalContextOptions:** Context formatting options

## External Dependencies

### Azure Services

1. **Azure OpenAI**
   - GPT deployment for chat completion and embeddings
   - Configuration: Endpoint, Deployment Name, API Key

2. **Azure AI Search**
   - Vector indexing and hybrid search
   - Configuration: Endpoint, API Key, Index Name

3. **Azure Cosmos DB** (Planned)
   - Persistent conversation storage
   - Not yet implemented

4. **Azure Key Vault** (Production)
   - Secrets management
   - Accessed via managed identity

### External APIs

- **GitHub API:** Repository access and file retrieval
- Rate limits: 60 (unauthenticated) / 5000 (authenticated) per hour

### Frontend

- React 19 with TypeScript
- Server-Sent Events (SSE) for streaming responses
- Markdown rendering and syntax highlighting

## Data Flows

### 1. Copilot Query Flow (Standard)

```
User Query
    ↓
POST /api/copilot/ask (CopilotRequest)
    ↓
CodeCopilotService.AskAsync
    ├→ Validate request parameters
    ├→ CodeRetrievalService.RetrieveAsync
    │   ├→ QueryEmbeddingService: Generate query embedding
    │   ├→ HybridSearchService: Search code chunks
    │   └→ RetrievalResultProcessor: Process results
    ├→ CodeContextBuilder: Format context
    ├→ CodeAnswerGenerationService.GenerateAsync
    │   ├→ CopilotPromptBuilder: Construct prompt
    │   └→ AzureOpenAIService.ChatAsync: Get AI response
    └→ Return CopilotResponse
        ↓
Return HTTP 200 with response
        ↓
Frontend renders response
```

### 2. Repository Ingestion Flow

```
RepositoryIngestionRequest
    ↓
RepositoryIngestionService.IngestAsync
    ├→ GitHubRepositoryProvider: Fetch files
    ├→ SourceFileClassifier: Classify files
    ├→ For each file:
    │   ├→ SourceFileMetadataService: Extract metadata
    │   ├→ CodeChunkingService: Chunk code
    │   └→ AzureOpenAIEmbeddingService: Generate embeddings
    ├→ SearchDocumentIndexingService: Index chunks
    └→ Return RepositoryIngestionResponse with summary
```

### 3. Tool-Calling Flow (Alternative to Standard)

```
User Query
    ↓
POST /api/copilot/ask
    ↓
CodeCopilotService detects tool-calling service
    ├→ CopilotToolRegistry: Get available tools
    ├→ ToolCallingCodeAnswerGenerationService.GenerateAsync
    │   ├→ AzureOpenAIService: Call with tools (function calling)
    │   ├→ If tool called:
    │   │   ├→ Tool (e.g., CodeSearchTool): Execute
    │   │   └→ AzureOpenAIService: Continue with tool results
    │   └→ Generate final response
    └→ Return CopilotResponse
```

## Architectural Boundaries

1. **API-to-Service Boundary:**
   - APIs validate input and delegate to services
   - Services contain business logic and external integration
   - Clear request/response contracts via DTOs

2. **Service-to-External Boundary:**
   - All Azure service access via typed interfaces (IAzureOpenAIService, etc.)
   - External APIs accessed via registered HttpClient
   - Configuration injected via Options pattern

3. **Dependency Direction:**
   - Endpoints → Services → Interfaces → Implementations
   - No circular dependencies
   - Higher-level layers depend on abstractions, not concrete types

4. **Data Isolation:**
   - Each request is independent
   - Scoped services per request (via dependency injection)
   - No shared mutable state across requests

## Deployment Model

- **Single API Instance:** ASP.NET Core hosting (local or Azure App Service)
- **Frontend:** Static React SPA served separately or as part of API
- **External Services:** Azure-managed (OpenAI, AI Search)
- **Scaling:** Horizontal via multiple API instances; Azure services handle autoscaling

## Non-Functional Requirements

### Reliability
- Async/await throughout; proper cancellation token handling
- Graceful error handling with meaningful error responses
- Service isolation via dependency injection enables testing without external services

### Performance
- Code embeddings cached during indexing
- Hybrid search balances accuracy and performance
- Streaming responses for better perceived performance
- Configurable context size for retrieval

### Maintainability
- Feature-based service organization
- Interface-first design for testability
- Minimal APIs pattern reduces boilerplate
- Comprehensive test coverage (41 test files)

### Security
- Azure Key Vault for secrets (production)
- .NET User Secrets for local development
- API validation prevents injection attacks
- Secure GitHub token handling (not exposed in logs/errors)

## Current Implementation Status

### Fully Implemented
- Azure OpenAI integration (chat, streaming, embeddings)
- Repository ingestion pipeline
- Code chunking and embedding
- Azure AI Search indexing and retrieval
- Copilot orchestration
- Tool-based answer generation
- Architecture analysis
- Code review
- Test generation
- React frontend with streaming
- xUnit test suite
- GitHub Actions CI/CD

### Partially Implemented
- User approval and publishing workspace changes back to a repository (not implemented)

### Planned / Not Yet Implemented
- Persistent conversation storage (Cosmos DB)
- Azure DevOps integration
- Advanced observability and diagnostics
- Production deployment (infrastructure as code)

## Known Architectural Gaps

1. **Conversation Persistence:** Currently in-memory; Cosmos DB integration planned but not implemented
2. **Workspace Isolation:** Application-level isolation is implemented for workspace file operations; OS-level sandboxing, approval, and publishing are not
3. **Rate Limiting:** No per-user/per-API-key rate limiting implemented
4. **Caching:** Limited caching strategy; improvement opportunity
5. **Monitoring:** Application Insights configured but custom metrics not comprehensive
6. **Error Recovery:** Retry logic for transient failures not fully implemented

## Related Documents

- [System Context](system-context.md) - Users, actors, and external systems
- [Component Design](component-design.md) - Detailed component responsibilities
- [Data Flow](data-flow.md) - End-to-end data flow diagrams
- [Security](security.md) - Security boundaries and controls
- [Architecture Decision Records](adr/) - Significant design decisions
