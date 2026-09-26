namespace Copilot.Api.Models.Repository;

public sealed record SourceFileMetadata(
    string Repository,
    string Branch,
    string Path,
    string Language,
    string Extension,
    int LineCount,
    long CharacterCount,
    long ByteCount,
    string Content);
