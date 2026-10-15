---
applyTo: ["infrastructure/**/*", ".github/workflows/**/*", "Dockerfile*"]
description: Infrastructure and deployment configuration for Azure, Terraform, and CI/CD
---

# Infrastructure Instructions

This file documents infrastructure, deployment, and DevOps practices for the AI Software Engineer Copilot.

**Applies to:** `infrastructure/`, `.github/workflows/`, `Dockerfile*`

## Current State

**Status:** Infrastructure-as-Code structure is reserved but not yet implemented.

- Deployment to production Azure environment: **Not yet implemented**
- Terraform/Bicep configuration: **Not yet implemented**
- Docker image configuration: **Not yet implemented**
- Production secrets management: **Not yet implemented**

## Intended Infrastructure

The system is designed to run on Azure with the following services:

### Azure Services (Planned/Configured)

1. **Azure App Service or Container Apps**
   - Hosts the ASP.NET Core API
   - Configurable deployment slots for staging/production

2. **Azure OpenAI**
   - GPT model deployment for AI responses
   - Configuration: Endpoint, Deployment Name, API Key (via Key Vault)

3. **Azure AI Search**
   - Vector and hybrid search indexing
   - Retrieval of code context

4. **Azure Cosmos DB**
   - Persistent storage for conversation history (planned feature)
   - Global distribution and scalability

5. **Azure Key Vault**
   - Secrets management (API keys, connection strings)
   - Managed identity authentication

6. **Azure Container Registry**
   - Docker image storage for deployments

7. **Application Insights**
   - Monitoring, logging, and diagnostics

### Local Development Environment

No special infrastructure required for local development; see `docs/runbooks/local-development.md`.

## CI/CD Pipeline

**Current Implementation:** GitHub Actions workflow at `.github/workflows/ci.yml`

### Pipeline Stages

1. **Checkout:** Clone the repository
2. **.NET Restore:** `dotnet restore`
3. **.NET Build (Release):** `dotnet build --configuration Release`
4. **.NET Tests:** `dotnet test --configuration Release`
5. **Node.js Setup:** Install Node.js and npm
6. **Frontend Install:** `npm ci` in `src/frontend/`
7. **Frontend Build:** `npm run build` in `src/frontend/`

### Trigger

- Runs on push to `main` or `develop` branches
- Runs on all pull requests to `main` or `develop`

### Result

- Build artifacts are created but not deployed
- Failures block merging to main branches

## Deployment (Future Implementation)

When deployment is implemented, follow these principles:

1. **Infrastructure as Code**
   - Use Terraform or Bicep for all Azure resources
   - Version control all infrastructure definitions
   - Support multiple environments (dev, staging, production)
   - Use variable files for environment-specific configuration

2. **Secrets Management**
   - Store all secrets in Azure Key Vault
   - Use managed identity for API authentication
   - Never commit secrets to source control
   - Rotate secrets regularly

3. **Container Strategy**
   - Dockerfile for backend API
   - Dockerfile for frontend (if containerized separately)
   - Image versioning aligned with release tags
   - Multi-stage builds to minimize image size

4. **Environment Configuration**
   - `appsettings.{Environment}.json` for environment-specific settings
   - Environment variables from App Service configuration
   - Separate App Insights instances per environment

5. **Database Migrations** (when persistent storage is implemented)
   - Versioned migration scripts
   - Automated migrations on deployment with rollback capability
   - Backup strategy before deployments

6. **Monitoring and Logging**
   - Application Insights configuration
   - Custom metrics for business operations
   - Alert thresholds for failures and performance degradation
   - Log retention policies

7. **Scaling Strategy**
   - Horizontal scaling via App Service instances or Container Apps replicas
   - Azure AI Search scale units for retrieval performance
   - Connection pooling for Cosmos DB (when used)

## Planned Infrastructure Directory Structure

When infrastructure is implemented, the expected structure is:

```
infrastructure/
├── terraform/
│   ├── main.tf                  # Main resource definitions
│   ├── variables.tf             # Variable definitions
│   ├── outputs.tf               # Output values
│   ├── backend.tf               # State management
│   └── environments/
│       ├── dev.tfvars           # Dev environment variables
│       ├── staging.tfvars       # Staging environment variables
│       └── prod.tfvars          # Production environment variables
│
└── bicep/
    ├── main.bicep               # Main resource definitions
    └── modules/                 # Reusable modules
        ├── app-service.bicep
        ├── key-vault.bicep
        ├── search.bicep
        └── cosmos-db.bicep
```

**Alternative:** Bicep instead of Terraform if Microsoft-aligned infrastructure preferred.

## Docker Strategy

### Backend API Dockerfile

When containerization is implemented:

```dockerfile
# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS builder
WORKDIR /src
COPY ["Copilot.sln", "."]
COPY ["src/backend/Copilot.Api/Copilot.Api.csproj", "src/backend/Copilot.Api/"]
RUN dotnet restore
COPY . .
RUN dotnet publish -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=builder /app/publish .
ENV ASPNETCORE_URLS=http://+:5000
EXPOSE 5000
ENTRYPOINT ["dotnet", "Copilot.Api.dll"]
```

**Principles:**
- Multi-stage build to minimize final image size
- Base on official Microsoft .NET images
- Don't run as root in production
- Use specific image versions (not `latest`)

### Frontend Dockerfile

If frontend is containerized separately:

```dockerfile
FROM node:20-alpine AS builder
WORKDIR /app
COPY src/frontend/package*.json ./
RUN npm ci
COPY src/frontend/ .
RUN npm run build

FROM node:20-alpine
WORKDIR /app
COPY --from=builder /app/dist ./dist
COPY src/frontend/package*.json ./
RUN npm ci --only=production
EXPOSE 3000
CMD ["npm", "start"]
```

## Environment Configuration

### Local Development

See `docs/runbooks/local-development.md` for .NET User Secrets configuration.

### Staging/Production

Configuration via Azure App Service:

```
ASPNETCORE_ENVIRONMENT: "Production"
AzureOpenAI__Endpoint: "<value from Key Vault>"
AzureOpenAI__DeploymentName: "<value from Key Vault>"
AzureOpenAI__ApiKey: "<value from Key Vault>"
AzureSearch__Endpoint: "<value from Key Vault>"
AzureSearch__ApiKey: "<value from Key Vault>"
AzureSearch__IndexName: "<index name>"
```

Connection strings and secrets should come from Key Vault via managed identity.

## Security Considerations

1. **Network Security**
   - Use network security groups (NSGs) to restrict traffic
   - Enable HTTPS only; redirect HTTP to HTTPS
   - Consider Azure WAF for DDoS protection

2. **API Authentication** (Future)
   - Implement API key or OAuth2 authentication
   - Rate limiting per API key
   - Audit logging of API access

3. **Data Protection**
   - Enable encryption at rest for all storage services
   - Use TLS 1.2+ for data in transit
   - Implement field-level encryption for sensitive data (if applicable)

4. **Identity and Access**
   - Use managed identity for API-to-Azure service authentication
   - Least privilege principle for role assignments
   - Regular access reviews

5. **Compliance**
   - Ensure compliance with relevant regulations (GDPR, SOC 2, etc.)
   - Implement audit logging
   - Document security controls

## Disaster Recovery

When production is live:

1. **Backup Strategy**
   - Daily backups of Cosmos DB
   - Azure Search index backup via blob storage
   - 30-day retention minimum

2. **Recovery Time Objective (RTO)**
   - Aim for RTO < 4 hours
   - Automate recovery procedures

3. **Recovery Point Objective (RPO)**
   - Aim for RPO < 1 hour
   - Implement continuous replication where supported

## Cost Optimization

1. **Reserved Instances:** Consider for predictable workloads
2. **Auto-scaling:** Automatically scale based on demand
3. **Resource Cleanup:** Regular cleanup of unused resources
4. **Monitoring:** Track costs via Azure Cost Management

## Health Checks and Monitoring

Implement health check endpoints in the API:

```csharp
app.MapGet("/health", () => Results.Ok("Healthy"))
   .WithName("Health")
   .WithOpenApi();

app.MapGet("/health/detailed", async (HttpContext context) =>
{
    var healthChecks = await service.CheckAllServicesAsync();
    return healthChecks.AllHealthy ? Results.Ok(healthChecks) : Results.StatusCode(503);
})
.WithName("DetailedHealth")
.WithOpenApi();
```

Health checks should verify:

- API responsiveness
- Azure OpenAI connectivity
- Azure AI Search connectivity
- Database connectivity (when applicable)
- Cache connectivity (when applicable)

## Deployment Checklist

When deploying to a new environment:

- [ ] Infrastructure provisioned via Terraform/Bicep
- [ ] Secrets configured in Azure Key Vault
- [ ] Application configuration set in App Service
- [ ] Managed identity configured
- [ ] Role assignments for identity completed
- [ ] Application Insights configured
- [ ] Monitoring alerts configured
- [ ] Backup strategy verified
- [ ] Health checks responding
- [ ] HTTPS certificate configured and valid
- [ ] CORS configured appropriately
- [ ] API rate limiting configured
- [ ] Logging and diagnostics verified
- [ ] Smoke tests passing
- [ ] Load tests completed (for production)

## Links

- [Global Instructions](../copilot-instructions.md)
- [Local Development Runbook](../../docs/runbooks/local-development.md)
- [CI/CD Workflow](./../workflows/ci.yml)
- [Architecture Overview](../../docs/architecture/architecture-overview.md)
