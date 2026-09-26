using Copilot.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<IAzureOpenAIService, AzureOpenAIService>();

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

var app = builder.Build();

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