namespace Copilot.Api.Models.FileInspection;

public sealed record FileInspectionResponse(
    string Repository,
    string Branch,
    string Path,
    string Language,
    string Content);
