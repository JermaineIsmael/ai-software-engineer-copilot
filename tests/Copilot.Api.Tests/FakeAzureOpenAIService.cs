using Copilot.Api;
using Copilot.Api.Services;

namespace Copilot.Api.Tests;

public class FakeAzureOpenAIService : IAzureOpenAIService
{
    public Task<string> GetResponseAsync(
        List<ChatMessage> messages)
    {
        return Task.FromResult(
            "This is a test response from the fake AI service.");
    }

    public async IAsyncEnumerable<string> GetResponseStreamingAsync(
        List<ChatMessage> messages)
    {
        yield return "This ";
        await Task.Yield();

        yield return "is ";
        await Task.Yield();

        yield return "a ";
        await Task.Yield();

        yield return "streamed ";
        await Task.Yield();

        yield return "test response.";
    }
}