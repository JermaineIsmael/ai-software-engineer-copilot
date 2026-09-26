using Copilot.Api.Services;
using Microsoft.Extensions.Configuration;

namespace Copilot.Api.Tests;

public class AzureOpenAIServiceTests
{
    [Fact]
    public void Constructor_Throws_WhenEndpointIsMissing()
    {
        var configuration = CreateConfiguration(
            endpoint: null,
            apiKey: "test-api-key",
            deploymentName: "gpt-5-mini");

        var exception = Assert.Throws<InvalidOperationException>(
            () => new AzureOpenAIService(configuration));

        Assert.Equal(
            "Azure OpenAI endpoint is not configured.",
            exception.Message);
    }

    [Fact]
    public void Constructor_Throws_WhenApiKeyIsMissing()
    {
        var configuration = CreateConfiguration(
            endpoint: "https://example.openai.azure.com",
            apiKey: null,
            deploymentName: "gpt-5-mini");

        var exception = Assert.Throws<InvalidOperationException>(
            () => new AzureOpenAIService(configuration));

        Assert.Equal(
            "Azure OpenAI API key is not configured.",
            exception.Message);
    }

    [Fact]
    public void Constructor_Throws_WhenDeploymentNameIsMissing()
    {
        var configuration = CreateConfiguration(
            endpoint: "https://example.openai.azure.com",
            apiKey: "test-api-key",
            deploymentName: null);

        var exception = Assert.Throws<InvalidOperationException>(
            () => new AzureOpenAIService(configuration));

        Assert.Equal(
            "Azure OpenAI deployment name is not configured.",
            exception.Message);
    }

    [Fact]
    public void Constructor_Succeeds_WhenConfigurationIsValid()
    {
        var configuration = CreateConfiguration(
            endpoint: "https://example.openai.azure.com",
            apiKey: "test-api-key",
            deploymentName: "gpt-5-mini");

        var service = new AzureOpenAIService(configuration);

        Assert.NotNull(service);
    }

    private static IConfiguration CreateConfiguration(
        string? endpoint,
        string? apiKey,
        string? deploymentName)
    {
        var configurationValues =
            new Dictionary<string, string?>();

        if (endpoint != null)
        {
            configurationValues["AzureOpenAI:Endpoint"] =
                endpoint;
        }

        if (apiKey != null)
        {
            configurationValues["AzureOpenAI:ApiKey"] =
                apiKey;
        }

        if (deploymentName != null)
        {
            configurationValues["AzureOpenAI:DeploymentName"] =
                deploymentName;
        }

        return new ConfigurationBuilder()
            .AddInMemoryCollection(configurationValues)
            .Build();
    }
}