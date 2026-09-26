using System.Net;
using System.Net.Http.Json;
using System.Text;

namespace Copilot.Api.Tests;

public class ChatEndpointTests
    : IClassFixture<ApiFactory>
{
    private readonly HttpClient _client;

    public ChatEndpointTests(ApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Chat_ReturnsBadRequest_WhenMessagesAreEmpty()
    {
        var request = new
        {
            messages = Array.Empty<object>()
        };

        var response = await _client.PostAsJsonAsync(
            "/api/chat",
            request);

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);

        var body =
            await response.Content.ReadFromJsonAsync<ErrorResponse>();

        Assert.NotNull(body);

        Assert.Equal(
            "Please provide at least one message.",
            body.Error);
    }

    [Fact]
    public async Task Chat_ReturnsAiResponse_WhenRequestIsValid()
    {
        var request = new
        {
            messages = new[]
            {
                new
                {
                    role = "user",
                    content = "Explain dependency injection."
                }
            }
        };

        var response = await _client.PostAsJsonAsync(
            "/api/chat",
            request);

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var body =
            await response.Content.ReadFromJsonAsync<ChatResponse>();

        Assert.NotNull(body);

        Assert.Equal(
            "This is a test response from the fake AI service.",
            body.Message);
    }

    [Fact]
    public async Task StreamingChat_ReturnsBadRequest_WhenMessagesAreEmpty()
    {
        var request = new
        {
            messages = Array.Empty<object>()
        };

        var response = await _client.PostAsJsonAsync(
            "/api/chat/stream",
            request);

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);

        var body =
            await response.Content.ReadAsStringAsync();

        Assert.Contains(
            "data:",
            body);

        Assert.Contains(
            "Please provide at least one message.",
            DecodeBase64Data(body));
    }

    [Fact]
    public async Task StreamingChat_ReturnsStreamedResponse_WhenRequestIsValid()
    {
        var request = new
        {
            messages = new[]
            {
                new
                {
                    role = "user",
                    content = "Explain dependency injection."
                }
            }
        };

        var response = await _client.PostAsJsonAsync(
            "/api/chat/stream",
            request);

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var body =
            await response.Content.ReadAsStringAsync();

        Assert.Contains(
            "data:",
            body);

        Assert.Contains(
            "data: [DONE]",
            body);

        var decodedContent =
            DecodeAllBase64Data(body);

        Assert.Equal(
            "This is a streamed test response.",
            decodedContent);
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

        return Encoding.UTF8.GetString(
            Convert.FromBase64String(encodedData));
    }

    private static string DecodeAllBase64Data(
        string response)
    {
        var decoded = new StringBuilder();

        var lines = response.Split('\n');

        foreach (var line in lines)
        {
            if (!line.StartsWith("data:"))
            {
                continue;
            }

            var encodedData = line
                .Substring(5)
                .Trim();

            if (encodedData == "[DONE]")
            {
                continue;
            }

            decoded.Append(
                Encoding.UTF8.GetString(
                    Convert.FromBase64String(encodedData)));
        }

        return decoded.ToString();
    }

    private sealed class ChatResponse
    {
        public string Message { get; set; } = string.Empty;
    }

    private sealed class ErrorResponse
    {
        public string Error { get; set; } = string.Empty;
    }
}