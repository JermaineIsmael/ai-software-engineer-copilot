namespace Copilot.Api.Models.Workspace;

public sealed record FileModificationResult(
    string Path,
    FileOperation Operation,
    string? ContentHash);
