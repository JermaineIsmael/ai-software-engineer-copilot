# Documentation Index

This page provides navigation and quick reference for all AI Software Engineer Copilot documentation.

## Getting Started

**Start here if you're new to the project:**

1. [README.md](../README.md) - Project overview and quick start
2. [Local Development Setup](runbooks/local-development.md) - Development environment configuration
3. [GitHub Copilot Instructions](.github/copilot-instructions.md) - Repository-wide AI assistance guidelines

## Architecture and Design

**Understanding the system:**

- [Architecture Overview](architecture/architecture-overview.md) - High-level system design and components
- [System Context](architecture/system-context.md) - Users, actors, external systems, and boundaries
- [Component Design](architecture/component-design.md) - Detailed component descriptions and interfaces
- [Data Flow](architecture/data-flow.md) - End-to-end workflow diagrams
- [Security](architecture/security.md) - Security controls, threats, and mitigations

### Architecture Decision Records (ADRs)

- [ADR 0001: Minimal APIs](architecture/adr/0001-minimal-apis.md) - HTTP API architecture pattern

## Product and Requirements

**What we're building and why:**

- [Product Requirements](product/product-requirements.md) - Vision, use cases, and requirements
- [Feature Roadmap](product/feature-roadmap.md) - Planned features and sprint timeline
- **[Sprint Roadmap](product/sprint-roadmap.md)** - Detailed sprint plans, acceptance criteria, and release procedures (Sprints 1-10)
- [MVP Definition](product/mvp-definition.md) - MVP scope and readiness criteria

## Development

**How to build and maintain the system:**

- [Implementation Status](development/implementation-status.md) - Feature inventory and status tracking
- [Coding Standards](development/coding-standards.md) - C# and .NET conventions
- [Testing Strategy](development/testing-strategy.md) - Testing approach and patterns
- [Sprint History](development/sprint-history.md) *(Planned)* - Sprint records and delivery tracking

## Instructions and Guidance

**Detailed guidance for specific work:**

### Global Instructions
- [GitHub Copilot Instructions](.github/copilot-instructions.md) - Repository-wide AI guidance

### Feature-Specific Instructions
- [Backend Instructions](.github/instructions/backend.instructions.md) - ASP.NET Core and C# development
- [Testing Instructions](.github/instructions/tests.instructions.md) - xUnit and test patterns
- [Infrastructure Instructions](.github/instructions/infrastructure.instructions.md) - Azure, deployment, and DevOps

## Runbooks and Operations

**How-to guides for common tasks:**

- [Local Development Setup](runbooks/local-development.md) - Development environment configuration
- [Troubleshooting](runbooks/troubleshooting.md) - Common issues and solutions

## Quick Reference

### By Role

**Software Developer:**
1. Read [Local Development Setup](runbooks/local-development.md)
2. Read [Backend Instructions](.github/instructions/backend.instructions.md)
3. Read [Coding Standards](development/coding-standards.md)
4. Reference [Component Design](architecture/component-design.md)

**QA / Tester:**
1. Read [Testing Strategy](development/testing-strategy.md)
2. Read [Testing Instructions](.github/instructions/tests.instructions.md)
3. Reference [Implementation Status](development/implementation-status.md)

**Architect / Tech Lead:**
1. Read [Architecture Overview](architecture/architecture-overview.md)
2. Read [System Context](architecture/system-context.md)
3. Review [ADRs](architecture/adr/)
4. Reference [Data Flow](architecture/data-flow.md)
5. Review [Security](architecture/security.md)

**DevOps / Infrastructure:**
1. Read [Infrastructure Instructions](.github/instructions/infrastructure.instructions.md)
2. Reference [Local Development Setup](runbooks/local-development.md)
3. Review [Security](architecture/security.md)
4. Review [Implementation Status](development/implementation-status.md)

### By Task

**Setting up local development:**
- [Local Development Setup](runbooks/local-development.md)

**Implementing a new feature:**
1. [Product Requirements](product/product-requirements.md) (understand the need)
2. [Sprint Roadmap](product/sprint-roadmap.md) (find your sprint and task)
3. [Feature Roadmap](product/feature-roadmap.md) (check priority and dependencies)
4. [Backend Instructions](.github/instructions/backend.instructions.md) (implementation patterns)
5. [Coding Standards](development/coding-standards.md) (code quality)
6. [Testing Strategy](development/testing-strategy.md) (test approach)
7. [Component Design](architecture/component-design.md) (reference implementations)

**Fixing a bug:**
1. [Troubleshooting](runbooks/troubleshooting.md) (understand the issue)
2. [Backend Instructions](.github/instructions/backend.instructions.md) (locate and fix)
3. [Testing Strategy](development/testing-strategy.md) (verify fix)

**Reviewing code:**
1. [Coding Standards](development/coding-standards.md) (style and conventions)
2. [Backend Instructions](.github/instructions/backend.instructions.md) (architecture patterns)
3. [Testing Strategy](development/testing-strategy.md) (test quality)
4. [Security](architecture/security.md) (security considerations)

**Deploying to production:**
1. [Infrastructure Instructions](.github/instructions/infrastructure.instructions.md)
2. [Security](architecture/security.md)
3. [Implementation Status](development/implementation-status.md) (readiness)

## Documentation Structure

```
docs/
├── architecture/                    # System design and decisions
│   ├── architecture-overview.md     # High-level architecture
│   ├── system-context.md            # Users and boundaries
│   ├── component-design.md          # Component details
│   ├── data-flow.md                 # Workflow diagrams
│   ├── security.md                  # Security controls
│   └── adr/                         # Architecture Decision Records
│       └── 0001-minimal-apis.md
├── product/                         # Product definition
│   ├── product-requirements.md      # Vision and requirements
│   ├── feature-roadmap.md           # Sprint planning (high-level)
│   ├── sprint-roadmap.md            # Detailed sprint roadmap (Sprints 1-10)
│   └── mvp-definition.md            # MVP scope
├── development/                     # Development practices
│   ├── implementation-status.md     # Feature status tracking
│   ├── coding-standards.md          # Code conventions
│   ├── testing-strategy.md          # Testing approach
│   └── sprint-history.md            # Sprint records
└── runbooks/                        # Operational guides
    ├── local-development.md         # Setup instructions
    ├── troubleshooting.md           # Common issues
    └── README.md                    # Documentation index (this file)

.github/
├── copilot-instructions.md          # Global AI guidance
└── instructions/                    # Feature-specific guidance
    ├── backend.instructions.md      # .NET development
    ├── tests.instructions.md        # Testing patterns
    └── infrastructure.instructions.md  # DevOps
```

## Key Concepts

**Layered Architecture:**
- API Endpoints → Services → Interfaces → Implementations → External Services
- Clear separation of concerns and dependency flow

**Dependency Injection:**
- All services registered in `Program.cs`
- Dependencies injected via constructors
- Interfaces enable testing with fake implementations

**Async/Await Throughout:**
- All I/O operations are async
- CancellationToken propagation for graceful shutdown
- No `.Result` or `.Wait()`

**Feature Organization:**
- Services organized by feature in subdirectories
- Tests mirror source structure
- Clear boundaries between features

**Secure Secrets Management:**
- .NET User Secrets for local development
- Azure Key Vault for production
- No secrets committed to Git

## External References

- [.NET 8 Documentation](https://learn.microsoft.com/en-us/dotnet/fundamentals/)
- [ASP.NET Core Minimal APIs](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/minimal-apis)
- [xUnit.net Documentation](https://xunit.net/docs/getting-started/netfx)
- [Moq Documentation](https://github.com/moq/moq4)
- [React Documentation](https://react.dev)
- [Azure OpenAI Documentation](https://learn.microsoft.com/en-us/azure/cognitive-services/openai/)
- [Azure AI Search Documentation](https://learn.microsoft.com/en-us/azure/search/)

## Contributing

When contributing to this project:

1. Follow [Coding Standards](development/coding-standards.md)
2. Write tests per [Testing Strategy](development/testing-strategy.md)
3. Update relevant documentation
4. Reference [Backend Instructions](.github/instructions/backend.instructions.md)
5. Ensure no secrets are committed
6. Create PR with clear description

## Questions?

- Review relevant documentation
- Check [Troubleshooting](runbooks/troubleshooting.md)
- Review existing code and tests for examples
- Ask in team channel with context
