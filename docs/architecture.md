# AI Software Engineer Copilot — Architecture

## Overview

The AI Software Engineer Copilot is a production-oriented developer assistant designed to help software engineers with code understanding, repository intelligence, code review, architecture analysis, and developer workflows.

The initial implementation provides a React frontend, an ASP.NET Core API, and Azure OpenAI integration. Future iterations will add repository search, engineering tools, GitHub/Azure DevOps integration, observability, and persistent conversation data.

## Current Architecture

```text
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
│  • Streaming responses                         │
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

# Technology Stack

## Frontend
* React
* TypeScript
* Vite
* React Markdown
* React Syntax Highlighter

## Backend
* .NET 8
* ASP.NET Core Minimal APIs
* C#
* OpenAI .NET SDK

## AI
* Azure OpenAI
* Responses API
* Streaming responses

## Testing
* xUnit
* Moq
* ASP.NET Core integration testing
* Microsoft.AspNetCore.Mvc.Testing

## Development
* Visual Studio Code
* Git
* GitHub
* GitHub Actions

# Current Request Flow
## Standard Chat
```
Developer
   │
   │ POST /api/chat
   ▼
ASP.NET Core API
   │
   ▼
IAzureOpenAIService
   │
   ▼
AzureOpenAIService
   │
   ▼
Azure OpenAI
   │
   ▼
AI response
   │
   ▼
ASP.NET Core API
   │
   ▼
React UI
```

## Streaming Chat
```
Developer
   │
   │ POST /api/chat/stream
   ▼
ASP.NET Core API
   │
   ▼
AzureOpenAIService
   │
   │ Streaming response
   ▼
Azure OpenAI
   │
   │ Text deltas
   ▼
ASP.NET Core API
   │
   │ Server-Sent Events
   ▼
React UI
   │
   ▼
Incremental response rendering
```

# Configuration and Secrets

Azure OpenAI configuration is intentionally separated from source-controlled configuration.
Development secrets are stored using the .NET User Secrets system.
The application expects:
```
AzureOpenAI:Endpoint
AzureOpenAI:DeploymentName
AzureOpenAI:ApiKey
```
Secrets must never be committed to Git.

Production deployments will use Azure Key Vault and/or managed identity rather than application source code or committed configuration files.

# Testing Architecture
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

This allows API tests to execute without requiring Azure credentials or making external AI requests.

# Planned Architecture

Future sprints will extend the architecture:

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

# Planned Capabilities
## Sprint 1 — Foundation

* GitHub repository and project structure
* .NET 8 ASP.NET Core API
* React and TypeScript frontend
* Azure OpenAI integration
* Chat API
* Streaming AI responses
* Conversation history
* Markdown rendering
* Syntax highlighting
* Error handling
* Clear conversation functionality
* Backend unit and integration tests
* API failure-path testing
* Secure local development configuration with .NET User Secrets
* `.gitignore` and repository cleanup
* GitHub Actions CI
* Architecture and project documentation

## Sprint 2 — Repository Intelligence
* Repository ingestion
* Source-code parsing
* Chunking
* Embeddings
* Azure AI Search
* Retrieval-Augmented Generation
* Source citations

## Sprint 3 — Engineering Agent
* Tool calling
* Code search
* File inspection
* Code review
* Test generation
* Architecture analysis

## Sprint 4 — Developer Workflow
* GitHub integration
* Azure DevOps integration
* Pull request analysis
* Work item analysis
* Git history analysis
* Pipeline investigation

## Sprint 5 — Cloud and Production
* Application Insights
* Azure logs
* Deployment diagnostics
* Security analysis
* Evaluation framework
* Agent observability

## Sprint 6 — Portfolio
* Production-quality UI
* Architecture diagrams
* Demo workflow
* Documentation
* Technical case study
* Portfolio presentation

# Design Principles
## Separation of concerns

The API, AI integration, frontend, and future agent tools are separated so that individual components can evolve independently.

## Dependency inversion

The API depends on IAzureOpenAIService rather than directly depending on Azure OpenAI implementation details.

## Testability

External AI services are abstracted behind interfaces so automated tests do not require live Azure resources.

## Security

Secrets are kept outside source control and will eventually move to Azure-managed identity and Key Vault for production.

## Incremental architecture

The project starts with a minimal working AI application and progressively introduces RAG, tools, integrations, persistence, observability, and cloud infrastructure.