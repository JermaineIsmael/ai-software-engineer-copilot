namespace Copilot.Api.Models.FileInspection;

public sealed record FileInspectionRequest(
    string RepositoryUrl,
    string Path,
    string Branch = "main");
