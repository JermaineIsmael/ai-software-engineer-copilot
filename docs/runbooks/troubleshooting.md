# Troubleshooting

This guide documents known issues, diagnostic steps, and solutions for common problems in the AI Software Engineer Copilot development and deployment.

## Development Issues

### Issue: "Azure OpenAI service failed" or "Unauthorized (401)"

**Symptoms:**
- API returns HTTP 401 or 403 error
- Log message: "Authentication failed to Azure OpenAI"
- UI shows: "AI service temporarily unavailable"

**Likely Causes:**
1. Azure OpenAI credentials not configured
2. Credentials expired or revoked
3. Azure resource in wrong region
4. API quota exceeded

**Diagnostic Steps:**
```bash
# Verify User Secrets are set
dotnet user-secrets list --project src/backend/Copilot.Api

# Check expected keys:
# AzureOpenAI:Endpoint
# AzureOpenAI:DeploymentName
# AzureOpenAI:ApiKey
```

**Solutions:**
1. **Reconfigure credentials:**
   ```bash
   cd src/backend/Copilot.Api
   dotnet user-secrets set "AzureOpenAI:Endpoint" "https://<correct-endpoint>.openai.azure.com/"
   dotnet user-secrets set "AzureOpenAI:DeploymentName" "<correct-deployment>"
   dotnet user-secrets set "AzureOpenAI:ApiKey" "<correct-key>"
   ```

2. **Verify Azure resource exists:**
   - Log into Azure Portal
   - Verify Azure OpenAI resource is deployed
   - Verify model deployment exists (e.g., "gpt-4" or "gpt-35-turbo")
   - Check region availability (Azure OpenAI not in all regions)

3. **Check quota:**
   - Azure Portal → Resource → Monitoring → Metrics
   - Look for quota usage vs. limits
   - Request quota increase if at limit

---

### Issue: "Azure AI Search not available" or Search Returns No Results

**Symptoms:**
- `HybridSearchService` throws exception
- Search results empty even after indexing
- HTTP 500 from `/api/retrieval/search`

**Likely Causes:**
1. Azure AI Search endpoint misconfigured
2. Search index doesn't exist
3. Documents not indexed yet
4. API key wrong

**Diagnostic Steps:**
```bash
# Verify Search configuration
dotnet user-secrets list --project src/backend/Copilot.Api | grep -i search

# Expected keys:
# AzureSearch:Endpoint
# AzureSearch:ApiKey
# AzureSearch:IndexName
```

**Solutions:**
1. **Reconfigure Search credentials:**
   ```bash
   dotnet user-secrets set "AzureSearch:Endpoint" "https://<correct-instance>.search.windows.net/"
   dotnet user-secrets set "AzureSearch:ApiKey" "<correct-key>"
   dotnet user-secrets set "AzureSearch:IndexName" "code-chunks"
   ```

2. **Create and populate index:**
   - Call `/api/retrieval/ingest` endpoint with repository info
   - Wait for indexing to complete
   - Retry search query

3. **Verify index exists:**
   - Azure Portal → Search resource → Indexes
   - Should see "code-chunks" index
   - Check document count > 0

---

### Issue: Build Fails with "The type or namespace does not exist"

**Symptoms:**
- Compiler error: `error CS0246: The type or namespace 'X' could not be found`
- Red squiggles in IDE
- `dotnet build` fails

**Likely Causes:**
1. Missing using statement
2. Dependency not restored
3. Namespace path incorrect
4. Circular dependency

**Diagnostic Steps:**
```bash
# Restore dependencies
dotnet restore

# Clean and rebuild
dotnet clean
dotnet build
```

**Solutions:**
1. **Add missing using statement:**
   - Look at error message for namespace
   - Example: `using Copilot.Api.Services.Copilot;`

2. **Restore and rebuild:**
   ```bash
   dotnet restore
   dotnet build --verbose
   ```

3. **Check for circular dependencies:**
   - Use IDE "Go to Definition" (F12) to verify namespace paths
   - Review service organization for proper layering

---

### Issue: Tests Fail with "NullReferenceException" or "Service Not Registered"

**Symptoms:**
- Test throws `NullReferenceException`
- Error: `No service for type 'IXxxService' has been registered`
- All tests in file fail with same error

**Likely Causes:**
1. Service not registered in DI container
2. Test factory not including service registration
3. Circular dependency in DI setup
4. Mock not configured correctly

**Diagnostic Steps:**
```bash
# Run single test with verbose output
dotnet test tests/Copilot.Api.Tests/YourTest.cs --logger "console;verbosity=detailed"

# Check Program.cs for service registration
grep "services.Add" src/backend/Copilot.Api/Program.cs
```

**Solutions:**
1. **Register missing service in `Program.cs`:**
   ```csharp
   builder.Services.AddScoped<IYourService, YourService>();
   ```

2. **Check test factory:**
   - ApiFactory should use WebApplicationFactory
   - Verify it includes all service registrations from Program.cs

3. **Fix mock configuration:**
   ```csharp
   var mockService = new Mock<IYourService>();
   mockService.Setup(x => x.MethodAsync(It.IsAny<string>()))
       .ReturnsAsync(expectedValue);  // Return something, don't leave null
   ```

---

### Issue: "Port already in use" - Cannot Start API or Frontend

**Symptoms:**
- `dotnet run` fails with "Address already in use"
- Port 5000/5001 (backend) or 5174 (frontend) not available
- Previous process still running

**Diagnostic Steps:**
```bash
# Find process using port (Windows)
netstat -ano | findstr :5001

# Or (Linux/Mac)
lsof -i :5001
```

**Solutions:**
```bash
# Kill process using port (Windows)
taskkill /PID <PID_FROM_ABOVE> /F

# Or (Linux/Mac)
kill -9 <PID>

# Or use different port
dotnet run --project src/backend/Copilot.Api --urls "http://localhost:5002"
```

---

### Issue: Frontend React App Shows Blank Page or 404

**Symptoms:**
- Browser shows blank page
- Console errors about missing resources
- 404 for index.html or main.tsx

**Likely Causes:**
1. Frontend not running (`npm run dev`)
2. Wrong URL (not `http://localhost:5174`)
3. Build artifacts missing
4. Port configuration mismatch

**Diagnostic Steps:**
```bash
# Verify frontend is running
cd src/frontend
npm run dev

# Check browser developer console (F12)
# Look for errors in Console and Network tabs
```

**Solutions:**
1. **Start frontend dev server:**
   ```bash
   cd src/frontend
   npm run dev
   ```

2. **Use correct URL:**
   - Should be `http://localhost:5174` (not https)
   - Check terminal output for correct port

3. **Clear browser cache:**
   - Ctrl+Shift+Delete (Windows) or Cmd+Shift+Delete (Mac)
   - Hard refresh: Ctrl+Shift+R (Windows) or Cmd+Shift+R (Mac)

---

### Issue: GitHub API Returns 403 "API rate limit exceeded"

**Symptoms:**
- Log: `403 Forbidden` from GitHub API
- Repository ingestion fails
- Unauthenticated requests fail

**Likely Causes:**
1. GitHub rate limit exceeded (60 per hour unauthenticated, 5000 authenticated)
2. GitHub token not configured
3. Invalid token

**Solutions:**
1. **Create GitHub token:**
   - GitHub → Settings → Developer settings → Personal access tokens
   - Create new token with `repo` and `read:user` scopes
   - Set token in User Secrets:
     ```bash
     dotnet user-secrets set "GitHub:Token" "<token>"
     ```

2. **Wait for rate limit reset:**
   - Rate limits reset hourly
   - Use different GitHub account temporarily

3. **Implement local caching:**
   - Cache repository data to avoid repeated API calls
   - See [Component Design](../docs/architecture/component-design.md) for caching strategy

---

## Deployment Issues

### Issue: Application Fails to Start in Azure App Service

**Symptoms:**
- App Service shows "Application Error"
- Logs show startup errors
- Health check fails

**Likely Causes:**
1. Secrets not configured in Key Vault
2. Environment variables not set
3. Connection strings missing
4. Dependency version mismatch

**Solutions:**
1. **Check Application Insights logs:**
   - Azure Portal → App Service → Monitoring → Application Insights
   - Look for startup errors

2. **Verify configuration:**
   ```bash
   # In App Service Configuration (Azure Portal):
   # AzureOpenAI__Endpoint
   # AzureOpenAI__DeploymentName  
   # AzureOpenAI__ApiKey (from Key Vault)
   # AzureSearch__Endpoint
   # AzureSearch__ApiKey (from Key Vault)
   ```

3. **Restart App Service:**
   - Azure Portal → App Service → Restart
   - Check logs again

---

### Issue: "SSL Certificate" Error or HTTPS Not Working

**Symptoms:**
- Browser warning about certificate
- `curl` fails with SSL error
- Mixed content warning (HTTP + HTTPS)

**Likely Causes:**
1. Certificate not installed
2. HTTPS not configured
3. Redirect not working

**Solutions (Development):**
```bash
# Trust local certificate
dotnet dev-certs https --trust

# Regenerate if corrupted
dotnet dev-certs https --clean
dotnet dev-certs https
```

**Solutions (Production):**
- Use Azure managed certificates
- Configure in App Service custom domain settings
- Implement HTTPS redirect in middleware

---

## Performance Issues

### Issue: Queries Slow (>5 seconds response time)

**Symptoms:**
- User waits >5 seconds for response
- Timeout errors
- "Processing..." spinner hangs

**Likely Causes:**
1. Large repository causes slow embedding generation
2. Azure OpenAI under load
3. Network latency
4. Inefficient search query

**Diagnostic Steps:**
```bash
# Add timing to service methods
var sw = Stopwatch.StartNew();
// ... operation ...
_logger.LogInformation("Operation took {Elapsed}ms", sw.ElapsedMilliseconds);

# Monitor Azure metrics
# Azure Portal → Resource → Metrics → Response Time
```

**Solutions:**
1. **Optimize embedding queries:**
   - Cache query embeddings
   - Batch requests when possible
   - Consider smaller `top` parameter (fewer results)

2. **Monitor Azure service load:**
   - Check if Azure OpenAI/Search under load
   - Scale up if needed

3. **Optimize search queries:**
   - Ensure index is properly optimized
   - Review search parameters

---

### Issue: High Memory Usage or Out of Memory Exceptions

**Symptoms:**
- Process uses large amounts of RAM
- `OutOfMemoryException` thrown
- Application becomes slow/unresponsive

**Likely Causes:**
1. Loading large repository at once
2. Memory leak in service
3. Unbounded cache growth
4. Embedding vectors consume memory

**Solutions:**
1. **Implement pagination:**
   - Process large repositories in batches
   - Clear processed data

2. **Monitor memory usage:**
   - Visual Studio → Debug → Memory usage
   - Look for unreleased objects

3. **Limit cache size:**
   - Implement TTL for cached items
   - Remove old entries

---

## Data Issues

### Issue: Search Results Not Improving After Reindexing

**Symptoms:**
- New code not appearing in search
- Old results still showing
- Index seems stale

**Likely Causes:**
1. Old documents not deleted
2. Index not fully refreshed
3. Caching preventing new data

**Solutions:**
1. **Clear and reindex:**
   - Delete existing index
   - Create new index
   - Reindex all documents

2. **Clear cache:**
   - Restart application
   - Clear embedding cache

---

## Common Recommendations

### When Troubleshooting

1. **Gather Information:**
   - Error message and stack trace
   - What was being attempted
   - When did it start?
   - Is it reproducible?

2. **Check Logs:**
   - Application logs (see local-development.md)
   - Azure diagnostic logs (if production)
   - Browser console (F12 for frontend)

3. **Isolate the Issue:**
   - Is it frontend or backend?
   - Is it specific to one API endpoint?
   - Reproducible with specific data?

4. **Search for Similar Issues:**
   - Check existing GitHub issues
   - Search documentation
   - Review past troubleshooting notes

5. **Escalate if Needed:**
   - Gather complete repro steps
   - Include error logs
   - Document what was tried

## Related Documents

- [Local Development Setup](local-development.md)
- [Architecture Overview](../docs/architecture/architecture-overview.md)
- [Security](../docs/architecture/security.md)
