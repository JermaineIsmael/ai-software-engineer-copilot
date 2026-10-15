namespace Copilot.Api.Models.Workspace;

public sealed record FileModification(
    string Path,
    FileOperation Operation,
    string? Content = null,
    string? ExpectedContentHash = null);
