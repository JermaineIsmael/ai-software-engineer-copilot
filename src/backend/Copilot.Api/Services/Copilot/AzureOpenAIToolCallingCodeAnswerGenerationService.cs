using System.ClientModel;
using System.Text.Json;
using OpenAI;
using OpenAI.Chat;
using Copilot.Api.Services.Tools;

namespace Copilot.Api.Services.Copilot;

public sealed class AzureOpenAIToolCallingCodeAnswerGenerationService :
    IToolCallingCodeAnswerGenerationService
{
    private readonly IConfiguration _configuration;
    private readonly ICopilotToolRegistry _toolRegistry;

    public AzureOpenAIToolCallingCodeAnswerGenerationService(
        IConfiguration configuration,
        ICopilotToolRegistry toolRegistry)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(toolRegistry);

        _configuration = configuration;
        _toolRegistry = toolRegistry;
    }

    public async Task<string> GenerateAsync(
        string query,
        string repository,
        string branch,
        IReadOnlyCollection<ICopilotTool> tools,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(query);
        ArgumentException.ThrowIfNullOrWhiteSpace(repository);
        ArgumentException.ThrowIfNullOrWhiteSpace(branch);
        ArgumentNullException.ThrowIfNull(tools);

        cancellationToken.ThrowIfCancellationRequested();

        var endpoint =
            _configuration["AzureOpenAI:Endpoint"];

        var apiKey =
            _configuration["AzureOpenAI:ApiKey"];

        var deployment =
            _configuration["AzureOpenAI:Deployment"];

        if (string.IsNullOrWhiteSpace(endpoint))
        {
            throw new InvalidOperationException(
                "AzureOpenAI:Endpoint is not configured.");
        }

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new InvalidOperationException(
                "AzureOpenAI:ApiKey is not configured.");
        }

        if (string.IsNullOrWhiteSpace(deployment))
        {
            throw new InvalidOperationException(
                "AzureOpenAI:Deployment is not configured.");
        }

        var client = new ChatClient(
            deployment,
            new ApiKeyCredential(apiKey),
            new OpenAIClientOptions
            {
                Endpoint = new Uri(
                    endpoint.TrimEnd('/') + "/openai/v1/")
            });

        var messages =
            new List<OpenAI.Chat.ChatMessage>
            {
                new SystemChatMessage(
                    """
                    You are an AI software engineer copilot.

                    Answer questions about the supplied repository.

                    You have access to repository tools:
                    - code_search: search indexed repository code.
                    - file_inspection: inspect the complete contents of a specific file.

                    Decide when a tool is needed.
                    Prefer tools when repository evidence is required.
                    Do not invent repository implementation details.

                    Repository:
                    """ + repository +
                    """

                    Branch:
                    """ + branch),

                new UserChatMessage(query)
            };

        var options = new ChatCompletionOptions();

        foreach (var tool in tools)
        {
            options.Tools.Add(
                ChatTool.CreateFunctionTool(
                    functionName: tool.Name,
                    functionDescription: tool.Description,
                    functionParameters:
                        BinaryData.FromString(
                            tool.ParametersJsonSchema)));
        }

        for (var iteration = 0; iteration < 5; iteration++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var completionResult =
                await client.CompleteChatAsync(
                    messages,
                    options,
                    cancellationToken);

            var completion = completionResult.Value;

            if (completion.FinishReason == ChatFinishReason.Stop)
            {
                messages.Add(
                    new AssistantChatMessage(completion));

                return string.Join(
                    Environment.NewLine,
                    completion.Content
                        .Where(content =>
                            !string.IsNullOrWhiteSpace(content.Text))
                        .Select(content => content.Text));
            }

            if (completion.FinishReason !=
                ChatFinishReason.ToolCalls)
            {
                throw new InvalidOperationException(
                    $"Unexpected model finish reason: {completion.FinishReason}.");
            }

            messages.Add(
                new AssistantChatMessage(completion));

            foreach (var toolCall in completion.ToolCalls)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var tool =
                    _toolRegistry.GetTool(
                        toolCall.FunctionName);

                if (tool is null)
                {
                    messages.Add(
                        new ToolChatMessage(
                            toolCall.Id,
                            [
                                ChatMessageContentPart.CreateTextPart(
                                    JsonSerializer.Serialize(
                                        new
                                        {
                                            error =
                                                $"Unknown tool '{toolCall.FunctionName}'."
                                        }))
                            ]));

                    continue;
                }

                try
                {
                    var result =
                        await tool.ExecuteAsync(
                            toolCall.FunctionArguments.ToString(),
                            cancellationToken);

                    messages.Add(
                        new ToolChatMessage(
                            toolCall.Id,
                            [
                                ChatMessageContentPart.CreateTextPart(
                                    result)
                            ]));
                }
                catch (Exception ex)
                    when (ex is ArgumentException ||
                          ex is JsonException)
                {
                    messages.Add(
                        new ToolChatMessage(
                            toolCall.Id,
                            [
                                ChatMessageContentPart.CreateTextPart(
                                    JsonSerializer.Serialize(
                                        new
                                        {
                                            error = ex.Message
                                        }))
                            ]));
                }
            }
        }

        throw new InvalidOperationException(
            "The maximum number of tool-calling iterations was reached.");
    }
}
