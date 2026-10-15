namespace Copilot.Api.Services.Workspace;

internal sealed class WorkspaceOperationLockLease : IDisposable
{
    private readonly Action _release;
    private int _disposed;

    public WorkspaceOperationLockLease(Action release)
    {
        _release = release;
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) == 0)
        {
            _release();
        }
    }
}
