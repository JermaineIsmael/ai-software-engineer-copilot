namespace Copilot.Api.Models.Workspace;

public sealed record Patch(
    IReadOnlyList<FileModification> Files);
