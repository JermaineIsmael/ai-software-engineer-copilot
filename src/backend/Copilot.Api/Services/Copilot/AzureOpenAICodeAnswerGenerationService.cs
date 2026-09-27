#pragma warning disable OPENAI001

using OpenAI;
using OpenAI.Chat;
using System.ClientModel;

namespace Copilot.Api.Services.Copilot;

public sealed class AzureOpenAICodeAnswerGenerationService
    : ICodeAnswerGenerationService
{
    private readonly ChatClient _client;

    public AzureOpenAICodeAnswerGenerationService(
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var endpoint =
            configuration["AzureOpenAI:Endpoint"];

        var apiKey =
            configuration["AzureOpenAI:ApiKey"];

        var deploymentName =
            configuration["AzureOpenAI:DeploymentName"];

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

        if (string.IsNullOrWhiteSpace(deploymentName))
        {
            throw new InvalidOperationException(
                "Azure OpenAI deployment name is not configured.");
        }

        var options = new OpenAIClientOptions
        {
            Endpoint = new Uri(
                $"{endpoint.TrimEnd('/')}/openai/v1/")
        };

        _client = new ChatClient(
            deploymentName,
            new ApiKeyCredential(apiKey),
            options);
    }

    public async Task<string> GenerateAsync(
        string query,
        string context,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            throw new ArgumentException(
                "Query cannot be empty.",
                nameof(query));
        }

        if (string.IsNullOrWhiteSpace(context))
        {
            throw new ArgumentException(
                "Context cannot be empty.",
                nameof(context));
        }

        cancellationToken.ThrowIfCancellationRequested();

        var messages = new List<OpenAI.Chat.ChatMessage>
        {
            new SystemChatMessage(
                CopilotPromptBuilder.BuildSystemPrompt()),

            new UserChatMessage(
                $"""
                Repository context:

                {context}

                User question:

                {query}
                """)
        };

        var response =
            await _client.CompleteChatAsync(
                messages,
                cancellationToken: cancellationToken);

        var completion = response.Value;

        if (completion.Content.Count == 0)
        {
            throw new InvalidOperationException(
                "Azure OpenAI returned an empty response.");
        }

        var answer = string.Join(
            Environment.NewLine,
            completion.Content
                .Where(content => !string.IsNullOrWhiteSpace(content.Text))
                .Select(content => content.Text));

        if (string.IsNullOrWhiteSpace(answer))
        {
            throw new InvalidOperationException(
                "Azure OpenAI returned no text content.");
        }

        return answer;
    }
}
