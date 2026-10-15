# Data Flow

This document describes the end-to-end data flows and interactions in the AI Software Engineer Copilot system.

## 1. Copilot Query - Standard Flow (RAG)

**Trigger:** User submits a query via React UI

```
User Query
"How does authentication work in this repo?"
    │
    ▼ POST /api/copilot/ask
┌────────────────────────────────────────────┐
│ HTTP Request Handler                       │
│ (CopilotEndpoints.cs)                      │
│                                            │
│ 1. Deserialize CopilotRequest JSON         │
│ 2. Validate parameters (query, repo, etc.) │
│ 3. Create logger                           │
└────────┬───────────────────────────────────┘
         │
         ▼
    ┌────────────────────────────────────────────┐
    │ CodeCopilotService.AskAsync                │
    │ (Entry point to orchestration)             │
    │                                            │
    │ 1. Validate input parameters               │
    │ 2. Check if tool-calling service available │
    │ 3. Since tool-calling unavailable,         │
    │    use standard RAG path                   │
    └────────┬───────────────────────────────────┘
             │
             ▼
    ┌──────────────────────────────────┐
    │ CodeRetrievalService.RetrieveAsync │
    │ (Retrieve relevant code)           │
    │                                    │
    │ Parameters:                        │
    │  - query: "authentication"         │
    │  - repository: "owner/repo"        │
    │  - branch: "main"                  │
    │  - top: 5                          │
    └────────┬──────────────────────────┘
             │
             ├─────────────────────────────────────┐
             │                                     │
             ▼                                     ▼
    ┌──────────────────────┐        ┌─────────────────────┐
    │QueryEmbeddingService │        │ HybridSearchService │
    │.GetEmbeddingAsync    │        │                     │
    │                      │        │ 1. Receive query    │
    │Calls Azure OpenAI    │        │    embedding        │
    │embeddings endpoint   │        │ 2. Call vector      │
    │                      │        │    search           │
    │Returns: float[]      │        │ 3. Call keyword     │
    │vector               │        │    search           │
    └──────────┬───────────┘        │ 4. Merge/rank       │
               │                    │    results          │
               └─────────────────┬──┘
                                 │
                                 ▼
                    ┌─────────────────────────┐
                    │ Azure AI Search         │
                    │                         │
                    │ Index: code-chunks      │
                    │ Fields:                 │
                    │  - content              │
                    │  - file_path            │
                    │  - line_numbers         │
                    │  - vector               │
                    └─────────────────────────┘
                                 │
                                 ▼
                    ┌──────────────────────────┐
                    │ Search Results (5 items) │
                    │                          │
                    │ [Ranked by relevance]    │
                    │  1. AuthService.cs       │
                    │  2. JwtHandler.cs        │
                    │  3. Policy.cs            │
                    │  4. Login.cs             │
                    │  5. Claims.cs            │
                    └──────────────┬───────────┘
                                   │
                                   ▼
    ┌──────────────────────────────────────┐
    │ RetrievalResultProcessor             │
    │ (Post-process results)               │
    │                                      │
    │ 1. Score normalization               │
    │ 2. Deduplication                     │
    │ 3. Metadata enrichment                │
    └────────────┬─────────────────────────┘
                 │
                 ▼
    ┌──────────────────────────────────────┐
    │ Processed Results:                   │
    │ CodeRetrievalResponse                │
    │  - RetrievedCodeChunk[]              │
    │    • content                         │
    │    • filePath                        │
    │    • lineNumbers                     │
    │    • score                           │
    └────────────┬─────────────────────────┘
                 │
                 ▼ (Back to CodeRetrievalService)
    ┌──────────────────────────────────────────────┐
    │ CodeContextBuilder                           │
    │ (Build AI-ready context)                      │
    │                                               │
    │ 1. Format code chunks for AI consumption      │
    │ 2. Include file paths and line references     │
    │ 3. Truncate if necessary to stay within       │
    │    token limits                              │
    │                                               │
    │ Output: Formatted context string              │
    └─────────────────┬──────────────────────────────┘
                      │
                      ▼ (Back to CodeCopilotService)
    ┌─────────────────────────────────────────┐
    │ CodeAnswerGenerationService.GenerateAsync
    │ (Generate AI response)                  │
    │                                         │
    │ 1. CopilotPromptBuilder constructs:     │
    │    "Answer the user question about     │
    │     this code:\n{code_context}"        │
    │                                         │
    │ 2. Calls AzureOpenAIService.ChatAsync  │
    │                                         │
    │ 3. Receives AI response:                │
    │    "Authentication is implemented      │
    │     using JWT tokens in AuthService..." │
    └─────────────────┬───────────────────────┘
                      │
                      ▼
    ┌─────────────────────────────────────┐
    │ CopilotResponse Constructed          │
    │                                      │
    │ {                                    │
    │   "content": "Authentication is...", │
    │   "sources": [                       │
    │     {"filePath": "AuthService.cs",   │
    │      "lineNumbers": "10-45"},        │
    │     ...                              │
    │   ]                                  │
    │ }                                    │
    └─────────────────┬───────────────────┘
                      │
                      ▼ (Back to HTTP Handler)
    ┌──────────────────────────────────────┐
    │ HTTP Response Handler                │
    │                                      │
    │ 1. Serialize response to JSON        │
    │ 2. Return HTTP 200 OK                │
    │ 3. Send to frontend                  │
    └──────────────────────────────────────┘
                      │
                      ▼
    ┌──────────────────────────────────────┐
    │ React Frontend                       │
    │                                      │
    │ 1. Receive response JSON             │
    │ 2. Parse and format                  │
    │ 3. Render Markdown response          │
    │ 4. Display source attribution        │
    │ 5. Show syntax-highlighted code      │
    └──────────────────────────────────────┘
                      │
                      ▼
    ┌──────────────────────────────────────┐
    │ Developer sees answer with sources   │
    └──────────────────────────────────────┘
```

## 2. Copilot Query - Tool-Calling Flow (Alternative)

**Trigger:** When `IToolCallingCodeAnswerGenerationService` is registered

```
User Query
"What files implement authentication?"
    │
    ▼ POST /api/copilot/ask
CodeCopilotService.AskAsync
    │
    ├─ Detects tool-calling service available
    │
    ├─ CopilotToolRegistry.GetTools()
    │   Returns: [CodeSearchTool, FileInspectionTool]
    │
    ▼
IToolCallingCodeAnswerGenerationService.GenerateAsync
    │
    ├─ Calls AzureOpenAIService with tools
    │
    ├─ Azure OpenAI chooses to call tool:
    │  "CodeSearchTool(query='authentication')"
    │
    ▼
CodeSearchTool.ExecuteAsync
    │
    ├─ Performs hybrid search for "authentication"
    ├─ Returns: List of matching files
    │
    ▼
AzureOpenAIService continues with tool results
    │
    ├─ If needed, calls another tool or
    ├─ Generates final answer incorporating
    │  tool results
    │
    ▼
CopilotResponse with tool-generated content
```

## 3. Repository Ingestion Flow

**Trigger:** Admin initiates indexing of new repository

```
┌────────────────────────────┐
│RepositoryIngestionRequest │
│  owner: "myorg"            │
│  repo: "myrepo"            │
│  branch: "main"            │
└────────────┬───────────────┘
             │
             ▼
┌────────────────────────────────────┐
│ POST /api/retrieval/ingest         │
│ (RetrievalEndpoints handler)       │
│                                    │
│ 1. Validate repository params      │
│ 2. Create ingest service           │
│ 3. Call RepositoryIngestionService│
└────────┬─────────────────────────┘
         │
         ▼
┌──────────────────────────────────────────┐
│ RepositoryIngestionService.IngestAsync  │
│                                          │
│ Status: Ingesting myorg/myrepo main     │
└────────┬─────────────────────────────────┘
         │
         ▼
┌──────────────────────────────────────────┐
│ GitHubRepositoryProvider.GetFilesAsync   │
│                                          │
│ HTTPS GET https://api.github.com/...    │
│                                          │
│ Returns: RepositorySnapshot              │
│  files: [                                │
│    { path: "src/Auth.cs", ... },        │
│    { path: "src/Models.cs", ... },      │
│    ...                                   │
│  ]                                       │
└────────┬─────────────────────────────────┘
         │
         │ (For each file)
         ├─ SourceFileClassifier
         │  Determines: Language = "C#"
         │
         ├─ SourceFileMetadataService
         │  Extracts: dependencies, classes, functions
         │
         ├─ CodeChunkingService
         │  Splits file into chunks (e.g., 1000 tokens each)
         │
         │  Example chunks:
         │    Chunk 1: "public class AuthService { ... }"
         │    Chunk 2: "public async Task<Token> LoginAsync(...)"
         │    Chunk 3: "private void ValidateClaims(...)"
         │
         ├─ AzureOpenAIEmbeddingService
         │  For each chunk:
         │    POST to Azure OpenAI embeddings endpoint
         │    Returns: float[1536] (embedding vector)
         │
         │  Example:
         │    Chunk 1 embedding: [0.123, -0.456, 0.789, ...]
         │    Chunk 2 embedding: [0.234, -0.567, 0.890, ...]
         │    Chunk 3 embedding: [0.345, -0.678, 0.901, ...]
         │
         └─ SearchDocumentIndexingService
            For each chunk with embedding:
              Create CodeChunkSearchDocument:
              {
                "id": "src_Auth_1",
                "filePath": "src/Auth.cs",
                "content": "public class AuthService { ... }",
                "vector": [0.123, -0.456, 0.789, ...],
                "lineNumbers": "10-45",
                "repository": "myorg/myrepo",
                "branch": "main"
              }
            
              POST to Azure AI Search:
              PUT /indexes/code-chunks/docs/src_Auth_1
         │
         ▼
    ┌─────────────────────────────────────┐
    │ Azure AI Search                     │
    │                                     │
    │ Index: code-chunks                  │
    │ Total documents indexed:            │
    │  - 2547 code chunks                 │
    │  - 156 files processed              │
    │  - 12 files skipped (binary, etc.)  │
    │                                     │
    │ Status: Indexing complete           │
    └──────────────┬──────────────────────┘
                   │
                   ▼
    ┌───────────────────────────────────────┐
    │ RepositoryIngestionResponse           │
    │                                       │
    │ {                                     │
    │   "status": "success",                │
    │   "filesProcessed": 156,              │
    │   "chunksCreated": 2547,              │
    │   "embeddingsGenerated": 2547,        │
    │   "docsIndexed": 2547,                │
    │   "errors": []                        │
    │ }                                     │
    │                                       │
    │ Ingest time: 5 minutes 23 seconds     │
    └───────────────────────────────────────┘
```

## 4. Error Flow

**Trigger:** Azure OpenAI service fails

```
User Query
    │
    ▼
CodeCopilotService.AskAsync
    │
    ▼
CodeRetrievalService.RetrieveAsync (SUCCESS)
    │
    ▼
CodeAnswerGenerationService.GenerateAsync
    │
    ├─ AzureOpenAIService.ChatAsync
    │
    └─ Throws HttpRequestException: "429 Too Many Requests"
         │
         ├─ Service logs error with context
         │  "Failed to call Azure OpenAI: Rate limit exceeded"
         │
         └─ Exception propagates to endpoint handler
                │
                ├─ Catches generic exception
                │
                └─ Returns HTTP 500 with error response:
                   {
                     "error": "AI service temporarily unavailable.
                               Please try again in a moment."
                   }
                   │
                   ▼
                   Frontend displays error message to user
```

## 5. Cancellation Flow

**Trigger:** User closes browser tab or clicks stop

```
Long-running query (ingestion, etc.)
    │
    ▼
Frontend detects user cancellation
    │
    ├─ XHR abort() or fetch AbortController.abort()
    │
    ▼
HTTP request cancelled on transport layer
    │
    ▼
ASP.NET Core propagates CancellationToken.IsCancellationRequested
    │
    ▼
Service methods check cancellationToken
    │
    ├─ Might be inside Azure OpenAI call
    ├─ Might be inside search operation
    │
    ▼
Service throws OperationCanceledException
    │
    ▼
Endpoint handler catches and returns HTTP 499
    │
    ▼
Frontend handles 499 gracefully
```

## 6. Data Models Throughout Flows

### CopilotRequest (Input)
```json
{
  "query": "How does authentication work?",
  "repository": "owner/repo",
  "branch": "main",
  "top": 5
}
```

### CodeRetrievalResponse (Internal)
```json
{
  "chunks": [
    {
      "filePath": "src/Auth/AuthService.cs",
      "content": "public class AuthService { ... }",
      "lineNumbers": [10, 45],
      "score": 0.92
    },
    ...
  ]
}
```

### CopilotResponse (Output)
```json
{
  "content": "Authentication in this repo...",
  "sources": [
    {
      "filePath": "src/Auth/AuthService.cs",
      "lineNumbers": [10, 45]
    },
    ...
  ]
}
```

## Related Documents

- [Architecture Overview](architecture-overview.md)
- [Component Design](component-design.md)
