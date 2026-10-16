using Copilot.Api.Models.Workspace;
using Copilot.Api.Services.Workspace;

namespace Copilot.Api.Services.Tools;

public sealed class RunWorkspaceBuildTool : BuildTestExecutionToolBase
{
    public RunWorkspaceBuildTool(
        IBuildTestExecutionService executionService)
        : base(executionService)
    {
    }

    public override string Name => "run_workspace_build";

    public override BuildTestOperation Operation => BuildTestOperation.Build;

    public override string Description =>
        "Run `dotnet build` on one .sln or .csproj in a trusted local workspace; " +
        "the process is not OS-sandboxed. " +
        "Available only when the user approved one build/test run for this request.";
}
