namespace Copilot.Api.Services.Workspace;

public sealed class BuildTestExecutionLock : IDisposable
{
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    public Task<bool> TryAcquireAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return _semaphore.WaitAsync(0, cancellationToken);
    }

    public void Release()
    {
        _semaphore.Release();
    }

    public void Dispose()
    {
        _semaphore.Dispose();
    }
}
