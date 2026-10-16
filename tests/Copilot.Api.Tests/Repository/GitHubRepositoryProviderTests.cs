using System.Net;
using System.Text;
using Copilot.Api.Services.Repository;
using Microsoft.Extensions.Logging.Abstractions;

namespace Copilot.Api.Tests.Repository;

public sealed class GitHubRepositoryProviderTests
{
    [Fact]
    public async Task GetRepositoryAsync_ReturnsResolvedCommitAndDefaultBranch()
    {
        var requestedUris = new List<string>();
        using var httpClient = new HttpClient(
            new GitHubResponseHandler(
                request =>
                {
                    var requestUri = request.RequestUri!;
                    requestedUris.Add(requestUri.PathAndQuery);

                    if (requestUri.AbsolutePath == "/repos/example/repository")
                    {
                        return JsonResponse(
                            """{"default_branch":"main"}""");
                    }

                    if (requestUri.AbsolutePath.EndsWith(
                            "/commits/feature%2Fsafe",
                            StringComparison.OrdinalIgnoreCase))
                    {
                        return JsonResponse(
                            """{"sha":"0123456789abcdef0123456789abcdef01234567"}""");
                    }

                    if (requestUri.AbsolutePath.Contains(
                            "/git/trees/0123456789abcdef0123456789abcdef01234567",
                            StringComparison.OrdinalIgnoreCase))
                    {
                        return JsonResponse(
                            """
                            {
                              "tree": [
                                {
                                  "path": "Copilot.sln",
                                  "type": "blob"
                                },
                                {
                                  "path": "src/sample.cs",
                                  "type": "blob"
                                }
                              ]
                            }
                            """);
                    }

                    if (requestUri.PathAndQuery.Contains(
                            "contents/src%2Fsample.cs?ref=0123456789abcdef0123456789abcdef01234567",
                            StringComparison.OrdinalIgnoreCase))
                    {
                        return JsonResponse(
                            """{"content":"c2FtcGxl"}""");
                    }

                    if (requestUri.PathAndQuery.Contains(
                            "contents/Copilot.sln?ref=0123456789abcdef0123456789abcdef01234567",
                            StringComparison.OrdinalIgnoreCase))
                    {
                        return JsonResponse(
                            """{"content":"c29sdXRpb24="}""");
                    }

                    return new HttpResponseMessage(HttpStatusCode.NotFound);
                }))
        {
            BaseAddress = new Uri("https://api.github.com/")
        };

        var provider = new GitHubRepositoryProvider(
            httpClient,
            NullLogger<GitHubRepositoryProvider>.Instance);

        var snapshot = await provider.GetRepositoryAsync(
            "https://github.com/example/repository",
            "feature/safe");

        Assert.Equal(
            "0123456789abcdef0123456789abcdef01234567",
            snapshot.CommitSha);
        Assert.Equal("main", snapshot.DefaultBranch);
        Assert.Equal(2, snapshot.Files.Count);
        Assert.Contains(snapshot.Files, file => file.Path == "Copilot.sln");
        Assert.Contains(snapshot.Files, file => file.Content == "sample");
        Assert.Contains(
            requestedUris,
            uri => uri.Contains("/commits/feature%2Fsafe", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(
            requestedUris,
            uri => uri.Contains(
                "/git/trees/0123456789abcdef0123456789abcdef01234567",
                StringComparison.OrdinalIgnoreCase));
    }

    private static HttpResponseMessage JsonResponse(string json)
    {
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(
                json,
                Encoding.UTF8,
                "application/json")
        };
    }

    private sealed class GitHubResponseHandler(
        Func<HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(respond(request));
        }
    }
}
