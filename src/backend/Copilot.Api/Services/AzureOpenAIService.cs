#pragma warning disable OPENAI001

using OpenAI.Responses;
using OpenAI;
using System.ClientModel;

namespace Copilot.Api.Services;

public class AzureOpenAIService
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

    public async Task<string> GetResponseAsync(string message)
    {
        var result = await _client.CreateResponseAsync(
            _deploymentName,
            message);

        return result.Value.GetOutputText();
    }
}