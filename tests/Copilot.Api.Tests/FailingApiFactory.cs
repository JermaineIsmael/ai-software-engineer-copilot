using Copilot.Api.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace Copilot.Api.Tests;

public class FailingApiFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(
        IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            var serviceDescriptor =
                services.SingleOrDefault(
                    descriptor =>
                        descriptor.ServiceType ==
                        typeof(IAzureOpenAIService));

            if (serviceDescriptor != null)
            {
                services.Remove(serviceDescriptor);
            }

            services.AddSingleton<IAzureOpenAIService>(
                new FailingAzureOpenAIService());
        });
    }
}