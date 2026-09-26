using System.Text;
using Copilot.Api.Models.Repository;

namespace Copilot.Api.Services.Repository;

public sealed class SourceFileMetadataService : ISourceFileMetadataService
{
    private readonly ISourceFileClassifier _classifier;

    public SourceFileMetadataService(
        ISourceFileClassifier classifier)
    {
        _classifier = classifier;
    }

    public SourceFileMetadata Extract(RepositoryFile file)
    {
        ArgumentNullException.ThrowIfNull(file);

        var language = _classifier.GetLanguage(file.Path);
        var extension = _classifier.GetExtension(file.Path);
        var lineCount = GetLineCount(file.Content);
        var characterCount = file.Content.Length;
        var byteCount = Encoding.UTF8.GetByteCount(file.Content);

        return new SourceFileMetadata(
            file.Repository,
            file.Branch,
            file.Path,
            language,
            extension,
            lineCount,
            characterCount,
            byteCount,
            file.Content);
    }

    private static int GetLineCount(string content)
    {
        if (string.IsNullOrEmpty(content))
        {
            return 0;
        }

        return content.Count(character => character == '\n') +
               (content.EndsWith('\n') ? 0 : 1);
    }
}
