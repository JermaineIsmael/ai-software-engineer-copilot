namespace Copilot.Api.Services.Workspace;

public sealed class BuildTestExecutionApproval : IBuildTestExecutionApproval
{
    private int _approved;

    public void SetApproved(bool approved)
    {
        Interlocked.Exchange(ref _approved, approved ? 1 : 0);
    }

    public bool TryConsume()
    {
        return Interlocked.Exchange(ref _approved, 0) == 1;
    }
}
