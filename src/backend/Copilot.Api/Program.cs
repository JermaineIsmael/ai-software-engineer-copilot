using System.Net.Http.Headers;
using Copilot.Api.Endpoints;
using Copilot.Api.Models.Repository;
using Copilot.Api.Services;
using Copilot.Api.Services.Repository;
using Copilot.Api.Services.Tools;
using Copilot.Api.Services.TestGeneration;
using Copilot.Api.Services.Search;
using Copilot.Api.Services.Retrieval;
using Copilot.Api.Services.Copilot;
using Copilot.Api.Services.ArchitectureAnalysis;
using Copilot.Api.Services.FileInspection;
using Copilot.Api.Services.CodeReview;



var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddOptions<Copilot.Api.Configuration.AzureSearchOptions>()
    .Bind(builder.Configuration.GetSection(
        Copilot.Api.Configuration.AzureSearchOptions.SectionName));

builder.Services
    .AddOptions<Copilot.Api.Configuration.RetrievalOptions>()
    .Bind(builder.Configuration.GetSection(
        Copilot.Api.Configuration.RetrievalOptions.SectionName));

builder.Services
    .AddOptions<Copilot.Api.Configuration.RetrievalContextOptions>()
    .Bind(builder.Configuration.GetSection(
        Copilot.Api.Configuration.RetrievalContextOptions.SectionName));


builder.Services.AddSingleton<IAzureOpenAIService, AzureOpenAIService>();
builder.Services.AddSingleton<IEmbeddingService, AzureOpenAIEmbeddingService>();
builder.Services.AddScoped<IQueryEmbeddingService, QueryEmbeddingService>();
builder.Services.AddScoped<ICodeRetrievalService, CodeRetrievalService>();
builder.Services.AddScoped<IRetrievalResultProcessor, RetrievalResultProcessor>();
builder.Services.AddScoped<ICodeContextBuilder, CodeContextBuilder>();

builder.Services.AddScoped<
    ICodeAnswerGenerationService,
    AzureOpenAICodeAnswerGenerationService>();

builder.Services.AddScoped<
    ICodeCopilotService,
    CodeCopilotService>();


builder.Services.AddHttpClient<IRepositoryProvider, GitHubRepositoryProvider>(client =>
{
    client.BaseAddress = new Uri("https://api.github.com/");
    client.DefaultRequestHeaders.UserAgent.Add(
        new ProductInfoHeaderValue("AI-Software-Engineer-Copilot", "0.1"));
    client.DefaultRequestHeaders.Accept.Add(
        new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
});

builder.Services.AddSingleton<ISourceFileClassifier, SourceFileClassifier>();
builder.Services.AddScoped<ISourceFileMetadataService, SourceFileMetadataService>();
builder.Services.AddSingleton<ICodeChunkingService, CodeChunkingService>();
builder.Services.AddScoped<IRepositoryIngestionService, RepositoryIngestionService>();
builder.Services.AddScoped<IFileInspectionService, FileInspectionService>();

builder.Services.AddScoped<CodeSearchTool>();
builder.Services.AddScoped<FileInspectionTool>();

builder.Services.AddScoped<ICopilotTool>(serviceProvider =>
    serviceProvider.GetRequiredService<CodeSearchTool>());

builder.Services.AddScoped<ICopilotTool>(serviceProvider =>
    serviceProvider.GetRequiredService<FileInspectionTool>());

builder.Services.AddScoped<ICopilotToolRegistry, CopilotToolRegistry>();

builder.Services.AddScoped<
    IToolCallingCodeAnswerGenerationService,
    AzureOpenAIToolCallingCodeAnswerGenerationService>();
builder.Services.AddSingleton<ISearchIndexDefinitionService, SearchIndexDefinitionService>();
builder.Services.AddSingleton<ISearchIndexManagementService, SearchIndexManagementService>();
builder.Services.AddSingleton<ISearchDocumentIndexingService, SearchDocumentIndexingService>();
builder.Services.AddSingleton<IVectorSearchService, VectorSearchService>();
builder.Services.AddSingleton<IHybridSearchService, HybridSearchService>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
    {
        policy
            .WithOrigins("http://localhost:5174")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

builder.Services.AddScoped<ICodeReviewService, CodeReviewService>();
builder.Services.AddScoped<
    ITestGenerationService,
    TestGenerationService>();
builder.Services.AddScoped<
    IArchitectureAnalysisService,
    ArchitectureAnalysisService>();

var app = builder.Build();

if (!app.Environment.IsEnvironment("Testing"))
{
    using var scope = app.Services.CreateScope();

    var indexManagementService =
        scope.ServiceProvider.GetRequiredService<ISearchIndexManagementService>();

    await indexManagementService.EnsureIndexAsync();
}

app.UseHttpsRedirection();
app.UseCors("Frontend");

app.MapPost("/api/chat", async (
    ChatRequest request,
    IAzureOpenAIService aiService) =>
{
    if (request.Messages == null || request.Messages.Count == 0)
    {
        return Results.BadRequest(new ErrorResponse
        {
            Error = "Please provide at least one message."
        });
    }

    try
    {
        var response = await aiService.GetResponseAsync(
            request.Messages);

        return Results.Ok(new ChatResponse
        {
            Message = response
        });
    }
    catch (Exception ex)
    {
        app.Logger.LogError(
            ex,
            "Error processing chat request.");

        return Results.Problem(
            title: "Copilot request failed",
            detail: "The Copilot was unable to process your request.",
            statusCode: StatusCodes.Status500InternalServerError);
    }
});

app.MapPost("/api/chat/stream", async (
    HttpContext context,
    ChatRequest request,
    IAzureOpenAIService aiService) =>
{
    context.Response.ContentType = "text/event-stream";
    context.Response.Headers.CacheControl = "no-cache";

    if (request.Messages == null || request.Messages.Count == 0)
    {
        context.Response.StatusCode = StatusCodes.Status400BadRequest;

        await context.Response.WriteAsync(
            "data: " +
            Convert.ToBase64String(
                System.Text.Encoding.UTF8.GetBytes(
                    "Please provide at least one message.")) +
            "\n\n");

        await context.Response.Body.FlushAsync();

        return;
    }

    try
    {
        await foreach (var chunk in
            aiService.GetResponseStreamingAsync(
                request.Messages))
        {
            var encodedChunk = Convert.ToBase64String(
                System.Text.Encoding.UTF8.GetBytes(chunk));

            await context.Response.WriteAsync(
                $"data: {encodedChunk}\n\n");

            await context.Response.Body.FlushAsync();
        }

        await context.Response.WriteAsync(
            "data: [DONE]\n\n");

        await context.Response.Body.FlushAsync();
    }
    catch (Exception ex)
    {
        app.Logger.LogError(
            ex,
            "Error streaming Copilot response.");

        if (!context.Response.HasStarted)
        {
            context.Response.StatusCode =
                StatusCodes.Status500InternalServerError;
        }

        var errorMessage =
            "The Copilot was unable to process your request.";

        var encodedError = Convert.ToBase64String(
            System.Text.Encoding.UTF8.GetBytes(errorMessage));

        await context.Response.WriteAsync(
            $"data: {encodedError}\n\n");

        await context.Response.Body.FlushAsync();
    }
});

app.MapPost("/api/repositories/ingest", async (
    RepositoryIngestionRequest request,
    IRepositoryIngestionService ingestionService,
    CancellationToken cancellationToken) =>
{
    if (string.IsNullOrWhiteSpace(request.RepositoryUrl))
    {
        return Results.BadRequest(new ErrorResponse
        {
            Error = "Repository URL is required."
        });
    }

    if (string.IsNullOrWhiteSpace(request.Branch))
    {
        return Results.BadRequest(new ErrorResponse
        {
            Error = "Branch is required."
        });
    }

    try
    {
        var snapshot = await ingestionService.IngestAsync(
            request.RepositoryUrl,
            request.Branch,
            cancellationToken);

        return Results.Ok(
            new RepositoryIngestionResponse(
                snapshot.Repository,
                snapshot.Branch,
                snapshot.Files.Count,
                snapshot.Chunks.Count,
                snapshot.Embeddings.Count,
                snapshot.Files,
                snapshot.Chunks,
                snapshot.Embeddings));
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(new ErrorResponse
        {
            Error = ex.Message
        });
    }
    catch (HttpRequestException ex)
    {
        app.Logger.LogError(
            ex,
            "Error retrieving repository {RepositoryUrl}.",
            request.RepositoryUrl);

        return Results.Problem(
            title: "Repository retrieval failed",
            detail: ex.Message,
            statusCode: StatusCodes.Status502BadGateway);
    }
    catch (Exception ex)
    {
        app.Logger.LogError(
            ex,
            "Error ingesting repository {RepositoryUrl}.",
            request.RepositoryUrl);

        return Results.Problem(
            title: "Repository ingestion failed",
            detail: "The repository could not be ingested.",
            statusCode: StatusCodes.Status500InternalServerError);
    }
});

app.MapRetrievalEndpoints();
app.MapFileInspectionEndpoints();
app.MapCopilotEndpoints();

app.MapCodeReviewEndpoints();
app.MapTestGenerationEndpoints();

app.MapArchitectureAnalysisEndpoints();

app.Run();

public partial class Program
{
}

public record ChatRequest(List<ChatMessage> Messages);

public record ChatMessage(
    string Role,
    string Content);

public class ChatResponse
{
    public string Message { get; set; } = string.Empty;
}

public class ErrorResponse
{
    public string Error { get; set; } = string.Empty;
}
