namespace Copilot.Api.Configuration;

public sealed class WorkspaceOptions
{
    public const string SectionName = "Workspace";

    public string? RootDirectory { get; set; }

    public int MaximumSnapshotFiles { get; set; } = 100_000;

    public long MaximumSnapshotBytes { get; set; } = 256L * 1024 * 1024;

    public int MaximumPatchFiles { get; set; } = 100;

    public long MaximumPatchBytes { get; set; } = 50L * 1024 * 1024;

    public string GetRootDirectory()
    {
        return string.IsNullOrWhiteSpace(RootDirectory)
            ? Path.Combine(
                Path.GetTempPath(),
                "ai-software-engineer-copilot",
                "workspaces")
            : RootDirectory;
    }
}
