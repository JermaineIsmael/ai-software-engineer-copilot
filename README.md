# AI Software Engineer Copilot

An AI-powered software engineering assistant built with .NET, React, Azure OpenAI, and Azure cloud technologies.

The goal of this project is to build a production-oriented AI engineering copilot that can assist developers with code understanding, repository intelligence, code review, architecture analysis, testing, and developer workflows.

## Overview

The AI Software Engineer Copilot is designed as an extensible AI platform rather than a generic chatbot.

The initial implementation provides a React frontend, an ASP.NET Core API, and Azure OpenAI integration. Future iterations will introduce repository indexing, Retrieval-Augmented Generation (RAG), engineering tools, GitHub and Azure DevOps integrations, cloud diagnostics, and observability.

### High-Level Architecture

```text
Developer
    │
    ▼
React / TypeScript
    │
    │ HTTP / SSE
    ▼
ASP.NET Core API
    │
    ▼
AI Agent / Orchestrator
    │
    ├──────────────► Azure OpenAI
    │
    ├──────────────► Azure AI Search
    │
    ├──────────────► GitHub
    │
    ├──────────────► Azure DevOps
    │
    ├──────────────► Cosmos DB
    │
    └──────────────► Azure Services
```    

## Current Capabilities

### AI Chat

- **Azure OpenAI Integration**
  - Connects the application to Azure OpenAI for AI-powered software engineering assistance.
- **Conversation History**
  - Maintains previous messages within the active conversation.
- **Streaming AI Responses**
  - Displays AI-generated responses incrementally.
- **Server-Sent Events (SSE)**
  - Provides real-time streaming between the backend API and frontend.
- **Markdown Rendering**
  - Supports structured Markdown responses from the AI.
- **Syntax Highlighting**
  - Displays source code using syntax highlighting for improved readability.

### API

- **ASP.NET Core Minimal APIs**
  - Provides a lightweight backend API architecture.
- **Chat API**
  - Provides an endpoint for standard AI responses.
- **Streaming Chat API**
  - Provides an endpoint for incremental AI responses.
- **Request Validation**
  - Validates incoming chat requests before processing.
- **Error Handling**
  - Provides controlled API and frontend error handling.
- **Dependency Injection**
  - Uses dependency injection to separate application components.
- **AI Service Abstraction**
  - Uses an interface-based AI service architecture to improve testability and maintainability.

### Testing

- **Unit Testing**
  - Uses xUnit for automated backend testing.
- **Service Isolation**
  - Uses Moq and service abstractions to isolate dependencies.
- **Integration Testing**
  - Tests ASP.NET Core API endpoints using the ASP.NET Core testing framework.
- **Failure-Path Testing**
  - Verifies API behavior when AI service requests fail.
- **Azure OpenAI-Independent Testing**
  - Automated tests do not require live Azure OpenAI credentials.

### Development

- **.NET User Secrets**
  - Stores local development credentials outside source-controlled configuration.
- **Repository Hygiene**
  - Prevents generated build artifacts and sensitive local files from being committed.
- **GitHub Actions CI**
  - Automatically validates builds and tests through continuous integration.
- **Release Build Validation**
  - Verifies that the backend can be compiled using the Release configuration.
- **Frontend Production Build Validation**
  - Verifies that the React application can be built for production.

## Technology Stack

### Backend

- C#
- .NET 8
- ASP.NET Core
- Minimal APIs
- OpenAI .NET SDK

### Frontend

- React
- TypeScript
- Vite
- React Markdown
- React Syntax Highlighter

### AI and Azure

- Azure OpenAI
- Azure AI Search
- Azure Cosmos DB
- Azure Service Bus
- Azure Key Vault
- Application Insights
- Azure Container Apps

### DevOps

- Git
- GitHub
- GitHub Actions
- Docker
- Terraform
- Bicep

### Testing

- xUnit
- Moq
- ASP.NET Core Integration Testing

## Project Structure

The project is organized into several major areas:

- **Backend**
  - Contains the ASP.NET Core API and AI service integrations.
- **Frontend**
  - Contains the React and TypeScript user interface.
- **Tests**
  - Contains automated unit and integration tests.
- **Documentation**
  - Contains architecture and technical documentation.
- **Infrastructure**
  - Reserved for Azure infrastructure and Infrastructure-as-Code definitions.
- **Scripts**
  - Reserved for development, deployment, and automation scripts.
- **GitHub Workflows**
  - Contains continuous integration and future deployment workflows.

```
ai-software-engineer-copilot/
│
├── src/
│   ├── backend/
│   │   └── Copilot.Api/
│   │       ├── Services/
│   │       ├── Program.cs
│   │       └── Copilot.Api.csproj
│   │
│   └── frontend/
│       ├── src/
│       ├── package.json
│       └── vite.config.ts
│
├── tests/
│   └── Copilot.Api.Tests/
│       ├── ApiFactory.cs
│       ├── AzureOpenAIServiceTests.cs
│       ├── ChatEndpointTests.cs
│       ├── ChatEndpointFailureTests.cs
│       ├── FakeAzureOpenAIService.cs
│       └── FailingAzureOpenAIService.cs
│
├── docs/
│   └── architecture.md
│
├── infrastructure/
│
├── scripts/
│
├── .github/
│   └── workflows/
│       └── ci.yml
│
├── .gitignore
├── Copilot.sln
├── README.md
└── LICENSE
```

## Getting Started

### Prerequisites

The project requires:

- .NET 8 SDK
- Node.js 20 or later
- npm
- Git
- An Azure subscription with access to Azure OpenAI for live AI functionality

### Configuration

Local Azure OpenAI configuration uses the .NET User Secrets system.

The application requires configuration for:

- Azure OpenAI Endpoint
- Azure OpenAI Deployment Name
- Azure OpenAI API Key

Sensitive credentials must never be committed to source control.

### Running the Application

The application consists of two primary components:

- **ASP.NET Core Backend**
  - Provides the API and Azure OpenAI integration.
- **React Frontend**
  - Provides the browser-based user interface.

Both components run independently during local development.

### Testing

The backend test suite can be executed locally without requiring live Azure OpenAI credentials.

Automated testing covers:

- AI service behavior
- API endpoints
- Streaming functionality
- Request validation
- API failure scenarios
- Dependency injection and service isolation

### Continuous Integration

GitHub Actions is used to validate changes automatically.

The CI process validates:

1. .NET dependency restoration
2. .NET compilation
3. Backend automated tests
4. Frontend dependency installation
5. Frontend production build

## Architecture

### Current Architecture
```
┌───────────────────────────────────────────────┐
│                  Developer                    │
│                                               │
│             Browser / React UI                │
└──────────────────────┬────────────────────────┘
                       │
                       │ HTTP / SSE
                       ▼
┌───────────────────────────────────────────────┐
│              ASP.NET Core API                 │
│                                               │
│  /api/chat                                    │
│  /api/chat/stream                             │
│                                               │
│  Request validation                           │
│  Conversation handling                        │
│  Streaming response                           │
└──────────────────────┬────────────────────────┘
                       │
                       ▼
┌───────────────────────────────────────────────┐
│            AI Service Layer                   │
│                                               │
│          AzureOpenAIService                   │
│                                               │
│  • Chat completion                            │
│  • Streaming responses                        │
│  • Conversation context                       │
└──────────────────────┬────────────────────────┘
                       │
                       │ HTTPS
                       ▼
┌───────────────────────────────────────────────┐
│                Azure OpenAI                   │
│                                               │
│              GPT Deployment                   │
└───────────────────────────────────────────────┘
```

### Configuration and Security
Azure OpenAI configuration is intentionally separated from source-controlled application configuration.

Development credentials are stored using .NET User Secrets.

The application expects the following configuration values:
```
AzureOpenAI:Endpoint
AzureOpenAI:DeploymentName
AzureOpenAI:ApiKey
```

Secrets must never be committed to Git.

For production deployments, the planned architecture uses:

- Azure Key Vault
- Managed Identity
- Azure-hosted application configuration
- Secretless authentication where supported

### Testing Architecture

The backend uses dependency injection to isolate the Azure OpenAI service from the API layer.
```

                 ┌─────────────────────┐
                 │     API Endpoint    │
                 └──────────┬──────────┘
                            │
                            ▼
                 ┌─────────────────────┐
                 │ IAzureOpenAIService │
                 └──────────┬──────────┘
                            │
                  ┌─────────┴─────────┐
                  ▼                   ▼
          Production Service     Fake Service
                  │                   │
                  ▼                   ▼
            Azure OpenAI         Automated Tests
```
This architecture allows automated tests to execute without requiring Azure credentials or making external AI requests.

## Roadmap

### Sprint 1 — Foundation

- GitHub repository and project structure
- .NET 8 ASP.NET Core API
- React and TypeScript frontend
- Azure OpenAI integration
- Chat API
- Streaming AI responses
- Conversation history
- Markdown rendering
- Syntax highlighting
- Error handling
- Clear conversation functionality
- Backend unit and integration tests
- API failure-path testing
- Secure local development configuration
- Repository cleanup
- GitHub Actions CI
- Architecture documentation

### Sprint 2 — Repository Intelligence

- Repository ingestion
- Source-code parsing
- Code chunking
- Embeddings
- Azure AI Search
- Retrieval-Augmented Generation
- Source citations

### Sprint 3 — Engineering Agent

- Tool calling
- Code search
- File inspection
- Code review
- Test generation
- Architecture analysis

### Sprint 4 — Developer Workflow

- GitHub integration
- Azure DevOps integration
- Pull request analysis
- Work item analysis
- Git history analysis
- Pipeline investigation

### Sprint 5 — Cloud and Production

- Application Insights
- Azure logs
- Deployment diagnostics
- Security analysis
- Evaluation framework
- Agent observability

### Sprint 6 — Portfolio

- Production-quality user interface
- Architecture diagrams
- Demonstration workflow
- Technical documentation
- Technical case study
- Portfolio presentation

## Architecture

The architecture documentation describes how the application evolves from an AI chat application into a broader AI-powered software engineering platform.

The architecture will progressively introduce:

- Azure OpenAI
- Azure AI Search
- Retrieval-Augmented Generation
- AI agent orchestration
- Developer tools
- GitHub integration
- Azure DevOps integration
- Cosmos DB
- Service Bus
- Application Insights
- Azure Key Vault
- Managed identities
- Cloud deployment

## Engineering Principles

### Separation of Concerns

Frontend, backend API, AI services, and future agent tools are separated so that individual components can evolve independently.

### Dependency Inversion

The API depends on abstractions rather than directly coupling application logic to external AI service implementations.

### Testability

External services are abstracted behind interfaces so automated tests can run without requiring live external AI services.

### Security

Development secrets are kept outside source control.

Production deployments are intended to use Azure-managed identity and Azure Key Vault rather than credentials stored in application source code.

### Incremental Architecture

The project is developed through progressively larger capabilities, allowing each stage to remain independently testable and maintainable.

## Project Goals

This project demonstrates practical experience across:

- Software engineering
- C# and .NET
- React and TypeScript
- Azure cloud architecture
- Azure OpenAI
- AI application development
- Retrieval-Augmented Generation
- AI agent design
- API design
- Automated testing
- DevOps and CI/CD
- Infrastructure as Code
- Cloud observability
- Secure application development

## Portfolio Objective

The project is intended to demonstrate the practical application of software engineering, cloud architecture, and AI engineering principles in a realistic developer-focused system.

The long-term objective is to evolve the application from a simple AI assistant into a production-oriented AI Software Engineer Copilot capable of interacting with source repositories, development platforms, cloud services, and engineering workflows.

## Future Architecture
The target architecture will evolve toward:
```
                         Developer
                             │
                             ▼
                    ┌─────────────────┐
                    │   React / Web   │
                    │       UI        │
                    └────────┬────────┘
                             │
                             ▼
                    ┌─────────────────┐
                    │   .NET API /    │
                    │ Agent Orchestr. │
                    └────────┬────────┘
                             │
          ┌──────────────────┼──────────────────┐
          │                  │                  │
          ▼                  ▼                  ▼
   ┌─────────────┐    ┌─────────────┐    ┌─────────────┐
   │ Azure OpenAI│    │ Azure AI    │    │ Tool Layer  │
   │             │    │ Search      │    │             │
   │ Reasoning   │    │ RAG / Code  │    │ GitHub      │
   │ Generation  │    │ Search      │    │ Azure DevOps│
   └─────────────┘    └─────────────┘    │ Cloud APIs  │
                                         └──────┬──────┘
                                                │
                             ┌──────────────────┼──────────────┐
                             │                  │              │
                             ▼                  ▼              ▼
                       ┌───────────┐      ┌──────────┐   ┌───────────┐
                       │ Cosmos DB │      │ Service  │   │ App       │
                       │           │      │ Bus      │   │ Insights  │
                       └───────────┘      └──────────┘   └───────────┘
```

## License

See the project `LICENSE` file for license information.

