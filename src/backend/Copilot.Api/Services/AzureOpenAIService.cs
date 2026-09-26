#pragma warning disable OPENAI001

using OpenAI.Responses;
using OpenAI;
using System.ClientModel;

namespace Copilot.Api.Services;

public class AzureOpenAIService : IAzureOpenAIService
{
    private readonly ResponsesClient _client;
    private readonly string _deploymentName;

    public AzureOpenAIService(IConfiguration configuration)
    {
        var endpoint = configuration["AzureOpenAI:Endpoint"];
        var apiKey = configuration["AzureOpenAI:ApiKey"];

        _deploymentName = configuration["AzureOpenAI:DeploymentName"]
            ?? throw new InvalidOperationException(
                "Azure OpenAI deployment name is not configured.");

        if (string.IsNullOrWhiteSpace(endpoint))
        {
            throw new InvalidOperationException(
                "Azure OpenAI endpoint is not configured.");
        }

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new InvalidOperationException(
                "Azure OpenAI API key is not configured.");
        }

        var options = new ResponsesClientOptions
        {
            Endpoint = new Uri(
                $"{endpoint.TrimEnd('/')}/openai/v1/")
        };

        _client = new ResponsesClient(
            new ApiKeyCredential(apiKey),
            options);
    }

    public async Task<string> GetResponseAsync(
        List<ChatMessage> messages)
    {
        var input = new List<ResponseItem>();

        foreach (var message in messages)
        {
            if (message.Role.Equals("user", StringComparison.OrdinalIgnoreCase))
            {
                input.Add(
                    ResponseItem.CreateUserMessageItem(message.Content));
            }
            else if (message.Role.Equals("assistant", StringComparison.OrdinalIgnoreCase))
            {
                input.Add(
                    ResponseItem.CreateAssistantMessageItem(message.Content));
            }
        }

        var result = await _client.CreateResponseAsync(
            _deploymentName,
            input);

        if (result == null || result.Value == null)
        {
            throw new InvalidOperationException(
                "Azure OpenAI returned an empty response.");
        }

        var outputText = result.Value.GetOutputText();

        if (string.IsNullOrWhiteSpace(outputText))
        {
            throw new InvalidOperationException(
                "Azure OpenAI returned no output text.");
        }

        return outputText;
    }
    
    public async IAsyncEnumerable<string> GetResponseStreamingAsync(
        List<ChatMessage> messages)
    {
        if (messages == null || messages.Count == 0)
        {
            throw new ArgumentException(
                "At least one chat message is required.",
                nameof(messages));
        }

        var input = new List<ResponseItem>();

        foreach (var message in messages)
        {
            if (message == null)
            {
                continue;
            }

            if (string.Equals(
                    message.Role,
                    "user",
                    StringComparison.OrdinalIgnoreCase))
            {
                if (!string.IsNullOrWhiteSpace(message.Content))
                {
                    input.Add(
                        ResponseItem.CreateUserMessageItem(
                            message.Content));
                }
            }
            else if (string.Equals(
                        message.Role,
                        "assistant",
                        StringComparison.OrdinalIgnoreCase))
            {
                if (!string.IsNullOrWhiteSpace(message.Content))
                {
                    input.Add(
                        ResponseItem.CreateAssistantMessageItem(
                            message.Content));
                }
            }
        }

        if (input.Count == 0)
        {
            throw new InvalidOperationException(
                "No valid messages were provided.");
        }

        var options = new CreateResponseOptions
        {
            Model = _deploymentName,
            StreamingEnabled = true
        };

        foreach (var item in input)
        {
            options.InputItems.Add(item);
        }

        await foreach (var update in
            _client.CreateResponseStreamingAsync(options))
        {
            if (update is StreamingResponseOutputTextDeltaUpdate textUpdate &&
                !string.IsNullOrEmpty(textUpdate.Delta))
            {
                yield return textUpdate.Delta;
            }
        }
    }    
}