using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using Copilot.Api.Models.Repository;

namespace Copilot.Api.Services.Repository;

public sealed class GitHubRepositoryProvider : IRepositoryProvider
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<GitHubRepositoryProvider> _logger;

    private static readonly HashSet<string> SupportedExtensions =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ".cs",
            ".csproj",
            ".json",
            ".yml",
            ".yaml",
            ".xml",
            ".sql",
            ".js",
            ".jsx",
            ".ts",
            ".tsx",
            ".html",
            ".css",
            ".scss",
            ".md",
            ".tf",
            ".bicep",
            ".dockerfile"
        };

    private static readonly string[] IgnoredPathSegments =
    [
        ".git",
        "bin",
        "obj",
        "node_modules",
        ".vs",
        "packages"
    ];

    public GitHubRepositoryProvider(
        HttpClient httpClient,
        ILogger<GitHubRepositoryProvider> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<RepositorySnapshot> GetRepositoryAsync(
        string repositoryUrl,
        string branch,
        CancellationToken cancellationToken = default)
    {
        var repository = ParseRepository(repositoryUrl);

        var treeUrl =
            $"repos/{repository.Owner}/{repository.Name}/git/trees/{Uri.EscapeDataString(branch)}?recursive=1";

        using var treeResponse = await _httpClient.GetAsync(
            treeUrl,
            cancellationToken);

        if (!treeResponse.IsSuccessStatusCode)
        {
            var statusCode = (int)treeResponse.StatusCode;

            throw new HttpRequestException(
                $"GitHub repository tree request failed with status code {statusCode}.");
        }

        await using var treeStream =
            await treeResponse.Content.ReadAsStreamAsync(cancellationToken);

        var tree = await JsonSerializer.DeserializeAsync<GitHubTreeResponse>(
            treeStream,
            cancellationToken: cancellationToken);

        if (tree?.Tree == null)
        {
            throw new InvalidOperationException(
                "GitHub returned an invalid or empty repository tree.");
        }

        var files = tree.Tree
            .Where(item => item.Type == "blob")
            .Where(item => IsSupportedFile(item.Path))
            .ToList();

        var repositoryFiles = new List<RepositoryFile>();

        foreach (var file in files)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                var content = await GetFileContentAsync(
                    repository.Owner,
                    repository.Name,
                    file.Path,
                    branch,
                    cancellationToken);

                repositoryFiles.Add(
                    new RepositoryFile(
                        $"{repository.Owner}/{repository.Name}",
                        branch,
                        file.Path,
                        GetLanguage(file.Path),
                        content));
            }
            catch (Exception ex) when (
                ex is HttpRequestException ||
                ex is JsonException)
            {
                _logger.LogWarning(
                    ex,
                    "Unable to retrieve repository file {FilePath}.",
                    file.Path);
            }
        }

        return new RepositorySnapshot(
            $"{repository.Owner}/{repository.Name}",
            branch,
            repositoryFiles);
    }

    private async Task<string> GetFileContentAsync(
        string owner,
        string name,
        string path,
        string branch,
        CancellationToken cancellationToken)
    {
        var url =
            $"repos/{owner}/{name}/contents/{Uri.EscapeDataString(path)}?ref={Uri.EscapeDataString(branch)}";

        using var response = await _httpClient.GetAsync(
            url,
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException(
                $"GitHub file request failed for '{path}' with status code {(int)response.StatusCode}.");
        }

        await using var stream =
            await response.Content.ReadAsStreamAsync(cancellationToken);

        var file = await JsonSerializer.DeserializeAsync<GitHubContentResponse>(
            stream,
            cancellationToken: cancellationToken);

        if (string.IsNullOrWhiteSpace(file?.Content))
        {
            throw new InvalidOperationException(
                $"GitHub returned no content for '{path}'.");
        }

        var normalizedContent =
            file.Content.Replace("\n", string.Empty)
                        .Replace("\r", string.Empty);

        var bytes = Convert.FromBase64String(normalizedContent);

        return System.Text.Encoding.UTF8.GetString(bytes);
    }

    private static bool IsSupportedFile(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return false;
        }

        var segments = path.Split(
            '/',
            StringSplitOptions.RemoveEmptyEntries);

        if (segments.Any(segment =>
            IgnoredPathSegments.Contains(segment)))
        {
            return false;
        }

        var fileName = Path.GetFileName(path);

        if (fileName.Equals(
            "Dockerfile",
            StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        var extension = Path.GetExtension(fileName);

        if (string.IsNullOrEmpty(extension))
        {
            return true;
        }

        return SupportedExtensions.Contains(extension);
    }

    private static string GetLanguage(string path)
    {
        var fileName = Path.GetFileName(path);

        if (fileName.Equals(
            "Dockerfile",
            StringComparison.OrdinalIgnoreCase))
        {
            return "Dockerfile";
        }

        return Path.GetExtension(fileName)
            .TrimStart('.')
            .ToLowerInvariant();
    }

    private static GitHubRepository ParseRepository(
        string repositoryUrl)
    {
        if (!Uri.TryCreate(
            repositoryUrl,
            UriKind.Absolute,
            out var uri))
        {
            throw new ArgumentException(
                "The repository URL is invalid.",
                nameof(repositoryUrl));
        }

        if (!uri.Host.Equals(
            "github.com",
            StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException(
                "Only GitHub repository URLs are currently supported.",
                nameof(repositoryUrl));
        }

        var segments = uri.AbsolutePath
            .Trim('/')
            .Split(
                '/',
                StringSplitOptions.RemoveEmptyEntries);

        if (segments.Length < 2)
        {
            throw new ArgumentException(
                "The GitHub repository URL must contain an owner and repository name.",
                nameof(repositoryUrl));
        }

        var name = segments[1];

        if (name.EndsWith(
            ".git",
            StringComparison.OrdinalIgnoreCase))
        {
            name = name[..^4];
        }

        return new GitHubRepository(
            segments[0],
            name);
    }

    private sealed record GitHubRepository(
        string Owner,
        string Name);

    private sealed class GitHubTreeResponse
    {
        [JsonPropertyName("tree")]
        public List<GitHubTreeItem> Tree { get; set; } = [];
    }

    private sealed class GitHubTreeItem
    {
        [JsonPropertyName("path")]
        public string Path { get; set; } = string.Empty;

        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;
    }

    private sealed class GitHubContentResponse
    {
        [JsonPropertyName("content")]
        public string Content { get; set; } = string.Empty;
    }
}
