# System Context

## Users and Actors

### Primary Actors

1. **Software Developer**
   - Uses the Copilot via React web UI or future IDE integration
   - Asks questions about code, architecture, and development tasks
   - Receives AI-assisted answers with code examples and explanations
   - May review generated code, tests, or architecture recommendations

2. **Development Team**
   - Owns and maintains source code repositories
   - Manages repository access and GitHub credentials
   - Reviews and approves suggested changes from Copilot
   - Tracks repository metadata and configuration

3. **DevOps/Infrastructure Administrator**
   - Deploys and manages Azure infrastructure
   - Configures secrets and credentials in Key Vault
   - Monitors application health and performance
   - Manages CI/CD pipelines

### Secondary Actors

- **GitHub:** Repository source of truth; provides repository data via API
- **Azure OpenAI:** Provides LLM reasoning and embeddings
- **Azure AI Search:** Provides vector and hybrid search capabilities

## System Boundary

### What the System Does

The Copilot system provides:

- **Code Understanding:** Understands existing code via repository ingestion and retrieval
- **Question Answering:** Answers developer questions about codebases
- **Code Review:** Reviews code snippets and suggests improvements
- **Architecture Analysis:** Analyzes and documents system architecture
- **Test Generation:** Generates test cases for given code
- **Repository Intelligence:** Indexes and searches code with semantic understanding

### What the System Does NOT Do

- Execute code or scripts
- Commit changes to repositories (currently planned for future)
- Modify production systems
- Store sensitive credentials in database (uses Key Vault)
- Make decisions without human review

## External Systems and Integrations

### Azure Services (Cloud Platform)

```
┌─────────────────────────────────────────┐
│        Copilot Backend (ASP.NET)        │
└──────────────┬────────────────────────┬─┘
               │                        │
               ▼                        ▼
        ┌──────────────┐         ┌──────────────┐
        │ Azure OpenAI │         │ Azure AI     │
        │              │         │ Search       │
        │• Chat Models │         │              │
        │• Embeddings  │         │• Indexing    │
        │              │         │• Retrieval   │
        └──────────────┘         └──────────────┘
               │                        │
               ├────────┬───────────────┤
               │        ▼               │
               │   ┌─────────────┐      │
               │   │ Application │      │
               │   │ Insights    │      │
               │   └─────────────┘      │
               │
               ▼
        ┌──────────────┐
        │ Azure Key    │
        │ Vault        │
        │ (secrets)    │
        └──────────────┘
```

### GitHub

```
┌─────────────────────────────────┐
│    Copilot Backend (ASP.NET)    │
└──────────────┬──────────────────┘
               │
        HTTPS / REST API
               │
               ▼
        ┌──────────────┐
        │   GitHub     │
        │   API        │
        │              │
        │• Repositories│
        │• Files       │
        │• Metadata    │
        └──────────────┘
```

### Frontend (React)

```
┌────────────────────┐
│   React Frontend   │
│ (TypeScript, Vite) │
└────────┬───────────┘
         │
    HTTP / WebSocket
    SSE (streaming)
         │
         ▼
┌────────────────────┐
│  ASP.NET Core API  │
└────────────────────┘
```

## System Interfaces

### Input Interfaces

1. **Web UI (React)**
   - User enters queries via text input
   - Optional context: repository, branch, file paths
   - Real-time streaming response display

2. **API Endpoints (Future)**
   - RESTful JSON API for programmatic access
   - Supports curl, SDKs, IDE integrations

3. **Configuration**
   - Secrets via Azure Key Vault (production)
   - .NET User Secrets (local development)
   - appsettings.json (default values)

### Output Interfaces

1. **HTTP Responses (REST)**
   - JSON response body with AI-generated text
   - Markdown formatting for structured responses
   - Source code highlighting metadata

2. **Server-Sent Events (SSE)**
   - Streaming responses for long-running operations
   - Real-time incremental delivery to frontend

3. **Error Responses**
   - Consistent error format with HTTP status codes
   - User-friendly error messages
   - Logging for diagnostics

## Quality Attributes

### Availability
- Target: 99.5% uptime (4 hours down/month)
- Failover via Azure App Service regional redundancy (future enhancement)

### Performance
- Query response time: < 5 seconds (90th percentile)
- Search latency: < 2 seconds for typical queries
- Streaming response start: < 1 second

### Security
- End-to-end HTTPS/TLS 1.2+
- Secrets stored in Azure Key Vault (production)
- API validation prevents injection attacks
- No sensitive data in logs

### Scalability
- Horizontal API scaling via App Service instances
- Azure AI Search and OpenAI scale transparently
- Stateless API enables multiple instances

### Maintainability
- Clear feature-based service organization
- Interface-first design for testing
- Comprehensive automated test suite
- Inline documentation for complex logic

### Extensibility
- Tool framework for adding new capabilities
- Service interfaces enable alternative implementations
- Dependency injection enables easy component swapping
- Minimal API pattern reduces boilerplate

## Deployment Context

### Development Environment
- Local machine with .NET 8 SDK and Node.js
- .NET User Secrets for Azure credentials
- No external Azure services required for testing

### Staging Environment
- Azure App Service
- Azure OpenAI and Azure AI Search instances
- Application Insights for monitoring
- Secrets from Azure Key Vault

### Production Environment
- Azure Container Apps or App Service
- Azure OpenAI and Azure AI Search (scaled instances)
- Cosmos DB for persistent conversation storage (planned)
- Application Insights with full diagnostics
- Azure WAF for DDoS protection (optional)

## Key Constraints and Assumptions

### Technical Constraints
- .NET 8.0 only (LTS version)
- Azure cloud platform required for AI and search services
- GitHub repositories must be accessible via GitHub API

### Operational Constraints
- Azure subscription required
- Azure OpenAI access requires approval (not available in all regions)
- GitHub PAT (Personal Access Token) required for private repositories

### Business Constraints
- Single-user SPA initially (no multi-tenant support)
- Query complexity limited by token limits of AI models
- Repository size limited by indexing and search service capacity

### Assumptions
- Users have valid GitHub repository access
- Azure credentials are securely stored
- Network connectivity to Azure services is stable
- Repositories are primarily in C#, .NET languages (extensible)

## Context Diagram (C4 Model - System Context Level)

```
┌─────────────────────────────────────────────────────┐
│                   Developer                         │
│              (User of the system)                   │
└────────────────────┬────────────────────────────────┘
                     │
                     │ Queries code via UI
                     │
        ┌────────────▼──────────────┐
        │  AI Software Engineer     │
        │  Copilot                  │
        │                           │
        │ System to assist          │
        │ developers with AI-       │
        │ powered code analysis,    │
        │ review, and generation    │
        └────────────┬──────────────┘
                     │
        ┌────────────┼──────────────┐
        │            │              │
        ▼            ▼              ▼
    ┌────────┐ ┌───────────┐  ┌──────────┐
    │ GitHub │ │Azure      │  │Azure AI  │
    │ API    │ │OpenAI     │  │Search    │
    │        │ │           │  │          │
    │Provides│ │Provides   │  │Provides  │
    │repo    │ │AI chat &  │  │code      │
    │data    │ │embeddings │  │indexing  │
    └────────┘ └───────────┘  └──────────┘
```

## Related Documents

- [Architecture Overview](architecture-overview.md) - High-level architecture and components
- [Component Design](component-design.md) - Detailed component descriptions
- [Data Flow](data-flow.md) - End-to-end workflow diagrams
