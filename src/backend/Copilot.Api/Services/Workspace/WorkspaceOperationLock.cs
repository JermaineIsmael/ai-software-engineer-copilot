using System.Collections.Concurrent;

namespace Copilot.Api.Services.Workspace;

public sealed class WorkspaceOperationLock
{
    private readonly ConcurrentDictionary<Guid, LockEntry> _locks = new();

    internal async Task<WorkspaceOperationLockLease> AcquireAsync(
        Guid workspaceId,
        CancellationToken cancellationToken)
    {
        while (true)
        {
            var entry = _locks.GetOrAdd(
                workspaceId,
                static _ => new LockEntry());

            lock (entry.Sync)
            {
                if (entry.Retired)
                {
                    continue;
                }

                entry.ReferenceCount++;
            }

            try
            {
                await entry.Semaphore.WaitAsync(cancellationToken);
                return new WorkspaceOperationLockLease(
                    () => Release(entry, workspaceId, releaseSemaphore: true));
            }
            catch
            {
                Release(entry, workspaceId, releaseSemaphore: false);
                throw;
            }
        }
    }

    private void Release(
        LockEntry entry,
        Guid workspaceId,
        bool releaseSemaphore)
    {
        if (releaseSemaphore)
        {
            entry.Semaphore.Release();
        }

        lock (entry.Sync)
        {
            entry.ReferenceCount--;

            if (entry.ReferenceCount == 0)
            {
                entry.Retired = true;
                _locks.TryRemove(workspaceId, out _);
                entry.Semaphore.Dispose();
            }
        }
    }

    private sealed class LockEntry
    {
        public object Sync { get; } = new();

        public SemaphoreSlim Semaphore { get; } = new(1, 1);

        public int ReferenceCount { get; set; }

        public bool Retired { get; set; }
    }
}
