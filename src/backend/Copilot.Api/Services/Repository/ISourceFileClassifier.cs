namespace Copilot.Api.Services.Repository;

public interface ISourceFileClassifier
{
    string GetLanguage(string path);

    string GetExtension(string path);
}
