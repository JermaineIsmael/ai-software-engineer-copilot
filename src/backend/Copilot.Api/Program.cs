using Copilot.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<AzureOpenAIService>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
    {
        policy
            .WithOrigins("http://localhost:5173")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var app = builder.Build();

app.UseHttpsRedirection();

app.UseCors("Frontend");

app.MapPost("/api/chat", async (
    ChatRequest request,
    AzureOpenAIService aiService) =>
{
    var response = await aiService.GetResponseAsync(request.Message);

    return Results.Ok(new ChatResponse
    {
        Message = response
    });
});

app.Run();

public record ChatRequest(string Message);

public class ChatResponse
{
    public string Message { get; set; } = string.Empty;
}