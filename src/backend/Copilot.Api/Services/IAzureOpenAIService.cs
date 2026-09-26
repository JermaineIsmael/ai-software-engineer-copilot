using Copilot.Api;

namespace Copilot.Api.Services;

public interface IAzureOpenAIService
{
    Task<string> GetResponseAsync(
        List<ChatMessage> messages);

    IAsyncEnumerable<string> GetResponseStreamingAsync(
        List<ChatMessage> messages);
}