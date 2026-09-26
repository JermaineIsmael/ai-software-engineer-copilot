namespace Copilot.Api.Services.Repository;

public sealed class SourceFileClassifier : ISourceFileClassifier
{
    private static readonly Dictionary<string, string> LanguageByExtension =
        new(StringComparer.OrdinalIgnoreCase)
        {
            [".cs"] = "csharp",
            [".csproj"] = "xml",
            [".json"] = "json",
            [".yml"] = "yaml",
            [".yaml"] = "yaml",
            [".xml"] = "xml",
            [".sql"] = "sql",
            [".js"] = "javascript",
            [".jsx"] = "javascript",
            [".ts"] = "typescript",
            [".tsx"] = "typescript",
            [".html"] = "html",
            [".css"] = "css",
            [".scss"] = "scss",
            [".md"] = "markdown",
            [".tf"] = "terraform",
            [".bicep"] = "bicep"
        };

    public string GetLanguage(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        var fileName = Path.GetFileName(path);
        var extension = GetExtension(path);

        if (fileName.Equals(
            "Dockerfile",
            StringComparison.OrdinalIgnoreCase))
        {
            return "dockerfile";
        }

        if (LanguageByExtension.TryGetValue(
            extension,
            out var language))
        {
            return language;
        }

        if (string.IsNullOrEmpty(extension) &&
            fileName.StartsWith(
                "README",
                StringComparison.OrdinalIgnoreCase))
        {
            return "markdown";
        }

        if (string.IsNullOrEmpty(extension) &&
            fileName.Equals(
                "LICENSE",
                StringComparison.OrdinalIgnoreCase))
        {
            return "text";
        }

        return "text";
    }

    public string GetExtension(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        var fileName = Path.GetFileName(path);

        if (fileName.Equals(
            "Dockerfile",
            StringComparison.OrdinalIgnoreCase))
        {
            return string.Empty;
        }

        return Path.GetExtension(fileName);
    }
}
