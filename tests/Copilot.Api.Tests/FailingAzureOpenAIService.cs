using Copilot.Api;
using Copilot.Api.Services;

namespace Copilot.Api.Tests;

public class FailingAzureOpenAIService : IAzureOpenAIService
{
    public Task<string> GetResponseAsync(
        List<ChatMessage> messages)
    {
        throw new InvalidOperationException(
            "Simulated Azure OpenAI failure.");
    }

    public async IAsyncEnumerable<string> GetResponseStreamingAsync(
        List<ChatMessage> messages)
    {
        await Task.Yield();

        throw new InvalidOperationException(
            "Simulated Azure OpenAI streaming failure.");

        // Required to make this an async iterator.
        yield break;
    }
}