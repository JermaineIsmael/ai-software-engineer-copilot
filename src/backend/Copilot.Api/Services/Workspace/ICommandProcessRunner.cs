using System.Diagnostics;

namespace Copilot.Api.Services.Workspace;

public interface ICommandProcessRunner
{
    Task<CommandProcessResult> RunAsync(
        ProcessStartInfo startInfo,
        TimeSpan timeout,
        int maximumOutputCharacters,
        CancellationToken cancellationToken = default);
}
