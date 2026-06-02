using System.Threading;
using System.Threading.Tasks;
using Proxyfan.Domain.Proxy;

namespace Proxyfan.Framework.Networking.Tests.Stubs;

/// <summary>
///     A configurable stub <see cref="IBackendHealthProbe" /> that returns the supplied
///     health value and records the probes it received.
/// </summary>
public sealed class StubBackendHealthProbe : IBackendHealthProbe
{
    private readonly TaskCompletionSource _probeStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);

    /// <summary>
    ///     Gets or sets the response the probe returns.
    /// </summary>
    public bool ResponseHealthy { get; set; }

    /// <summary>
    ///     Gets the number of probes received.
    /// </summary>
    public int ProbeCount { get; private set; }

    /// <summary>
    ///     Gets or sets an optional gate. When set, ProbeAsync awaits the task before returning.
    /// </summary>
    public Task? ProbeGate { get; set; }

    /// <summary>
    ///     Gets a task that completes when ProbeAsync has been entered. Useful for
    ///     deterministically waiting until the probe is in-flight from a test.
    /// </summary>
    public Task ProbeStarted => _probeStarted.Task;

    /// <inheritdoc />
    public async Task<bool> ProbeAsync(string host, int port, CancellationToken cancellationToken)
    {
        _ = (host, port, cancellationToken);
        ProbeCount++;
        _probeStarted.TrySetResult();
        if (ProbeGate is not null)
        {
            await ProbeGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        }

        return ResponseHealthy;
    }
}
