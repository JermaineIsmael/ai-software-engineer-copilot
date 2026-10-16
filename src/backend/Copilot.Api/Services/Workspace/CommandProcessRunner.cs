using System.ComponentModel;
using System.Diagnostics;
using System.Text;

namespace Copilot.Api.Services.Workspace;

public sealed class CommandProcessRunner : ICommandProcessRunner
{
    public async Task<CommandProcessResult> RunAsync(
        ProcessStartInfo startInfo,
        TimeSpan timeout,
        int maximumOutputCharacters,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(startInfo);

        if (timeout <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nameof(timeout),
                "The process timeout must be greater than zero.");
        }

        if (maximumOutputCharacters <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(maximumOutputCharacters),
                "The output limit must be greater than zero.");
        }

        cancellationToken.ThrowIfCancellationRequested();

        using var process = new Process
        {
            StartInfo = startInfo,
            EnableRaisingEvents = true
        };

        var stopwatch = Stopwatch.StartNew();
        try
        {
            if (!process.Start())
            {
                throw new InvalidOperationException(
                    $"Failed to start the approved '{startInfo.FileName}' process.");
            }
        }
        catch (Exception exception)
            when (exception is Win32Exception or
                IOException or
                UnauthorizedAccessException or
                InvalidOperationException)
        {
            throw new InvalidOperationException(
                $"Could not start the approved '{startInfo.FileName}' executable.",
                exception);
        }

        var output = new BoundedOutputBuffer(maximumOutputCharacters);
        using var timeoutSource = new CancellationTokenSource(timeout);
        using var waitSource = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken,
            timeoutSource.Token);
        var stdoutTask = CaptureAsync(
            process.StandardOutput,
            "stdout",
            output,
            waitSource.Token);
        var stderrTask = CaptureAsync(
            process.StandardError,
            "stderr",
            output,
            waitSource.Token);

        try
        {
            await process.WaitForExitAsync(waitSource.Token);
            await Task.WhenAll(stdoutTask, stderrTask);
        }
        catch (OperationCanceledException)
        {
            if (!process.HasExited)
            {
                await TerminateProcessTreeAsync(process);
            }

            try
            {
                await Task.WhenAll(stdoutTask, stderrTask);
            }
            catch (OperationCanceledException)
                when (waitSource.IsCancellationRequested)
            {
            }

            if (cancellationToken.IsCancellationRequested)
            {
                throw;
            }

            stopwatch.Stop();

            return new CommandProcessResult(
                TimedOut: true,
                ExitCode: null,
                stopwatch.ElapsedMilliseconds,
                output.ToString(),
                output.Truncated);
        }
        catch (Exception exception)
            when (exception is IOException or InvalidOperationException)
        {
            if (!process.HasExited)
            {
                await TerminateProcessTreeAsync(process);
            }

            throw new InvalidOperationException(
                "Failed while collecting build/test process output.",
                exception);
        }

        cancellationToken.ThrowIfCancellationRequested();
        stopwatch.Stop();

        return new CommandProcessResult(
            TimedOut: false,
            process.ExitCode,
            stopwatch.ElapsedMilliseconds,
            output.ToString(),
            output.Truncated);
    }

    private static async Task CaptureAsync(
        StreamReader reader,
        string streamName,
        BoundedOutputBuffer output,
        CancellationToken cancellationToken)
    {
        var buffer = new char[4096];

        while (true)
        {
            var count = await reader.ReadAsync(
                buffer.AsMemory(),
                cancellationToken);
            if (count == 0)
            {
                return;
            }

            output.Append(streamName, buffer.AsSpan(0, count));
        }
    }

    private static async Task TerminateProcessTreeAsync(Process process)
    {
        if (process.HasExited)
        {
            return;
        }

        try
        {
            process.Kill(entireProcessTree: true);
        }
        catch (InvalidOperationException)
            when (process.HasExited)
        {
            return;
        }

        await process.WaitForExitAsync(CancellationToken.None);
    }

    private sealed class BoundedOutputBuffer
    {
        private readonly int _maximumCharacters;
        private readonly StringBuilder _output = new();
        private readonly object _sync = new();
        private readonly HashSet<string> _streams = new(
            StringComparer.Ordinal);

        public BoundedOutputBuffer(int maximumCharacters)
        {
            _maximumCharacters = maximumCharacters;
        }

        public bool Truncated { get; private set; }

        public void Append(string streamName, ReadOnlySpan<char> content)
        {
            lock (_sync)
            {
                var prefix = $"[{streamName}] ";
                var remaining = _maximumCharacters - _output.Length;

                if (remaining <= 0)
                {
                    Truncated = true;
                    return;
                }

                if (_streams.Add(streamName))
                {
                    var prefixLength = Math.Min(prefix.Length, remaining);
                    _output.Append(prefix.AsSpan(0, prefixLength));
                    remaining -= prefixLength;
                }

                var contentLength = Math.Min(content.Length, remaining);
                _output.Append(content[..contentLength]);

                if (contentLength < content.Length)
                {
                    Truncated = true;
                }
            }
        }

        public override string ToString()
        {
            lock (_sync)
            {
                return _output.ToString();
            }
        }
    }
}
