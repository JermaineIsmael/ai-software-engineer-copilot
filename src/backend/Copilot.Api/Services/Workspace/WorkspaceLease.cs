using Copilot.Api.Models.Workspace;
using WorkspaceModel = Copilot.Api.Models.Workspace.Workspace;

namespace Copilot.Api.Services.Workspace;

public sealed class WorkspaceLease : IAsyncDisposable
{
    private readonly Func<ValueTask> _release;
    private int _disposed;

    internal WorkspaceLease(
        WorkspaceModel workspace,
        Func<ValueTask> release)
    {
        Workspace = workspace;
        _release = release;
    }

    public WorkspaceModel Workspace { get; }

    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposed, 1) == 0)
        {
            await _release();
        }
    }
}
