# Local Development Setup

This runbook provides step-by-step instructions for setting up a local development environment for the AI Software Engineer Copilot.

## Prerequisites

### Required Software

- **.NET 8.0 SDK** (LTS)
  - Download: https://dotnet.microsoft.com/download/dotnet/8.0
  - Verify: `dotnet --version` (should show 8.0.x)

- **Node.js 20 or later**
  - Download: https://nodejs.org
  - Verify: `node --version` (should show v20.x or higher)

- **Git**
  - Download: https://git-scm.com
  - Verify: `git --version`

### Optional but Recommended

- **Visual Studio Code**
  - C# Dev Kit extension
  - REST Client extension (for API testing)

- **Visual Studio 2022** (Community or higher)
  - C# and .NET workload

- **Postman** or **curl**
  - For testing API endpoints

## Repository Cloning

```bash
# Clone the repository
git clone https://github.com/JermaineIsmael15/developer-clone.git
cd developer-clone

# Verify directory structure
ls -la
# Should show: .github, .gitignore, Copilot.sln, README.md, docs, src, tests
```

## Backend Setup (.NET API)

### 1. Restore Dependencies

```bash
cd developer-clone
dotnet restore
```

This downloads all NuGet packages specified in the solution.

### 2. Verify Build

```bash
dotnet build
```

Verify no compiler errors. Should output:
```
Build succeeded.
```

### 3. Initialize User Secrets

The application requires Azure credentials for local development.

```bash
# Navigate to API project
cd src/backend/Copilot.Api

# Initialize User Secrets (one-time)
dotnet user-secrets init

# List secrets (should be empty initially)
dotnet user-secrets list
```

### 4. Configure Azure Credentials

You need valid Azure OpenAI and Azure AI Search access. Obtain from:
- Azure subscription (https://azure.microsoft.com)
- Azure Portal → Your resources

```bash
# Set Azure OpenAI configuration
dotnet user-secrets set "AzureOpenAI:Endpoint" "https://<your-instance>.openai.azure.com/"
dotnet user-secrets set "AzureOpenAI:DeploymentName" "<deployment-name>"
dotnet user-secrets set "AzureOpenAI:ApiKey" "<api-key>"

# Set Azure AI Search configuration
dotnet user-secrets set "AzureSearch:Endpoint" "https://<your-instance>.search.windows.net/"
dotnet user-secrets set "AzureSearch:ApiKey" "<api-key>"
dotnet user-secrets set "AzureSearch:IndexName" "code-chunks"

# Optional: GitHub token (for private repositories)
dotnet user-secrets set "GitHub:Token" "<personal-access-token>"

# Verify secrets are set
dotnet user-secrets list
```

**Notes:**
- Secrets are stored locally in `%APPDATA%\Microsoft\UserSecrets\` (Windows)
- Never commit secrets to Git
- Each developer maintains their own local secrets

### 5. Run Unit Tests

```bash
# From repository root
dotnet test

# Or specific test file
dotnet test tests/Copilot.Api.Tests/Services/Copilot/CodeCopilotServiceTests.cs
```

Expected: All tests pass (41 test files)

### 6. Run the Backend API

```bash
# From repository root
dotnet run --project src/backend/Copilot.Api

# Or use dotnet watch for auto-reload
dotnet watch --project src/backend/Copilot.Api
```

Backend will start on `https://localhost:5001` (HTTPS) or `http://localhost:5000` (HTTP).

**Verify:** Open browser to `https://localhost:5001` (you may see certificate warning; this is normal for local development)

## Frontend Setup (React)

### 1. Install Dependencies

```bash
cd src/frontend
npm install

# Or for exact reproducible install
npm ci
```

### 2. Development Server

```bash
# From frontend directory
npm run dev

# Or from repository root
cd src/frontend && npm run dev
```

Frontend will start on `http://localhost:5174` (or next available port if 5174 is in use).

**Output:**
```
  VITE v8.3.0  ready in ... ms

  ➜  Local:   http://localhost:5174/
```

### 3. Build for Production

```bash
npm run build

# Output: dist/ directory with optimized build
ls dist/

# Preview production build
npm run preview
```

## Integrated Development

### Terminal 1: Backend API

```bash
dotnet run --project src/backend/Copilot.Api
```

API running on `https://localhost:5001`

### Terminal 2: Frontend Dev Server

```bash
cd src/frontend
npm run dev
```

Frontend running on `http://localhost:5174`

### Terminal 3: Optional - Monitor Tests

```bash
# Run tests in watch mode (if supported)
dotnet watch test
```

### Access the Application

Open browser to: **http://localhost:5174**

You should see:
- Query input field
- Response display area
- Syntax highlighting for code

## Testing the API Manually

### Using curl

```bash
# Test basic copilot query
curl -X POST https://localhost:5001/api/copilot/ask \
  -H "Content-Type: application/json" \
  -d '{
    "query": "How does authentication work?",
    "repository": "owner/repo",
    "branch": "main",
    "top": 5
  }'
```

### Using REST Client in VS Code

Create `.vscode/test.rest` file:

```rest
@baseUrl = https://localhost:5001

### Test Copilot Endpoint
POST {{baseUrl}}/api/copilot/ask
Content-Type: application/json

{
  "query": "How does authentication work?",
  "repository": "owner/repo",
  "branch": "main",
  "top": 5
}

### Test Health Check
GET {{baseUrl}}/health
```

Click "Send Request" to execute.

## Common Issues and Solutions

### Issue: Certificate Error for HTTPS

**Symptom:** `SSL_ERROR_SELF_SIGNED_CERT`

**Solution:**
```bash
# Generate/trust local certificate (Windows)
dotnet dev-certs https --trust

# Or use HTTP for testing
# Modify ASPNETCORE_URLS=http://localhost:5000
```

### Issue: Port Already in Use

**Symptom:** "Address already in use"

**Solution:**
```bash
# Kill process using port
# Windows
netstat -ano | findstr :5001
taskkill /PID <PID> /F

# Linux/Mac
lsof -i :5001
kill -9 <PID>
```

### Issue: Azure Credentials Not Working

**Symptom:** `401 Unauthorized` or `InvalidAuthenticationTokenTenant`

**Solution:**
1. Verify credentials in Azure Portal
2. Check User Secrets are set correctly: `dotnet user-secrets list`
3. Ensure Azure resource is in correct region and available
4. Check quota/billing on Azure subscription

### Issue: Node/npm Version Mismatch

**Symptom:** `npm ERR! Node v16.x is not supported`

**Solution:**
```bash
# Update Node to 20+
# Windows: Use installer or nvm
nvm install 20
nvm use 20

# Verify
node --version
npm --version
```

### Issue: Tests Fail with "Service Not Found"

**Symptom:** `NullReferenceException` or `InvalidOperationException`

**Solution:**
- Tests should not require Azure credentials
- Use fake implementations: `FakeAzureOpenAIService`
- Verify `Program.cs` DI registration

## Development Workflow

### 1. Create Feature Branch

```bash
git checkout -b feature/my-feature
```

### 2. Make Changes

Edit files in `src/`, `tests/`, or frontend

### 3. Run Tests

```bash
dotnet test
```

All tests must pass

### 4. Run Linting

```bash
# C# linting
dotnet build

# Frontend linting
cd src/frontend
npm run lint
```

### 5. Commit Changes

```bash
git add .
git commit -m "Add my feature"
```

### 6. Push and Create PR

```bash
git push origin feature/my-feature
```

Create pull request on GitHub

## Useful Commands Reference

```bash
# Build
dotnet build
dotnet build -c Release

# Test
dotnet test
dotnet test --filter "TestName"
dotnet test --configuration Release

# Run
dotnet run --project src/backend/Copilot.Api
dotnet watch

# User Secrets
dotnet user-secrets init
dotnet user-secrets set "Key" "value"
dotnet user-secrets list
dotnet user-secrets clear

# Clean
dotnet clean

# Frontend
cd src/frontend
npm install
npm run dev
npm run build
npm run lint
```

## Debugging

### Debug Backend in VS Code

1. Install C# Dev Kit extension
2. Set breakpoint in code (click line number)
3. Run `dotnet run --project src/backend/Copilot.Api`
4. Step through code with F10/F11

### Debug Frontend in VS Code

1. Install Debugger for Firefox or Chrome
2. Run `npm run dev`
3. Open Developer Tools (F12)
4. Set breakpoints in Code tab
5. Step through code

### Debug API Calls

1. Use REST Client extension (VS Code)
2. Use browser Developer Tools (Network tab)
3. Use Postman or similar

## Performance Considerations

### For Development

- Backend startup: ~5-10 seconds
- First query: ~5 seconds (embeddings generation)
- Subsequent queries: ~2-3 seconds
- Frontend hot reload: <1 second

### Optimization Tips

- Cache embeddings locally during development
- Use smaller test repositories for faster iteration
- Disable logging in development if performance-critical

## Next Steps

- Read [Architecture Overview](../docs/architecture/architecture-overview.md)
- Read [Backend Instructions](../.github/instructions/backend.instructions.md)
- Read [Testing Strategy](../docs/development/testing-strategy.md)
- Review existing tests for patterns
- Start implementing features per [Feature Roadmap](../docs/product/feature-roadmap.md)

## Getting Help

- Check [Troubleshooting Runbook](troubleshooting.md)
- Review existing code and tests for examples
- Read documentation in `docs/` directory
- Ask in team channel with error details and steps to reproduce
