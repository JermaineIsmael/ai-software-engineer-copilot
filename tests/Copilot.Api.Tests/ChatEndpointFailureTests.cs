using System.Net;
using System.Net.Http.Json;

namespace Copilot.Api.Tests;

public class ChatEndpointFailureTests
    : IClassFixture<FailingApiFactory>
{
    private readonly HttpClient _client;

    public ChatEndpointFailureTests(
        FailingApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Chat_ReturnsInternalServerError_WhenAiServiceFails()
    {
        var request = new
        {
            messages = new[]
            {
                new
                {
                    role = "user",
                    content = "Test failure handling."
                }
            }
        };

        var response = await _client.PostAsJsonAsync(
            "/api/chat",
            request);

        Assert.Equal(
            HttpStatusCode.InternalServerError,
            response.StatusCode);

        var body =
            await response.Content.ReadAsStringAsync();

        Assert.Contains(
            "Copilot request failed",
            body);

        Assert.Contains(
            "The Copilot was unable to process your request.",
            body);
    }

    [Fact]
    public async Task StreamingChat_ReturnsInternalServerError_WhenAiServiceFails()
    {
        var request = new
        {
            messages = new[]
            {
                new
                {
                    role = "user",
                    content = "Test streaming failure handling."
                }
            }
        };

        var response = await _client.PostAsJsonAsync(
            "/api/chat/stream",
            request);

        Assert.Equal(
            HttpStatusCode.InternalServerError,
            response.StatusCode);

        var body =
            await response.Content.ReadAsStringAsync();

        Assert.Contains(
            "data:",
            body);

        Assert.Contains(
            "The Copilot was unable to process your request.",
            DecodeBase64Data(body));
    }

    private static string DecodeBase64Data(
        string response)
    {
        var line = response
            .Split('\n')
            .FirstOrDefault(x =>
                x.StartsWith("data:"));

        if (line == null)
        {
            return string.Empty;
        }

        var encodedData = line
            .Substring(5)
            .Trim();

        return System.Text.Encoding.UTF8.GetString(
            Convert.FromBase64String(encodedData));
    }
}