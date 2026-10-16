namespace Copilot.Api.Services.Workspace;

public interface IBuildTestExecutionApproval
{
    void SetApproved(bool approved);

    bool TryConsume();
}
