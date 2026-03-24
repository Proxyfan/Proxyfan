using System;
using System.Threading;
using System.Threading.Tasks;

namespace Proxyfan.Domain.Proxy.Tests.Stubs;

/// <summary>
///     A configurable stub for <see cref="IProxyListener" /> used in unit tests.
/// </summary>
internal sealed class StubProxyListener : IProxyListener
{
    private int? _boundPort;
    private Exception? _startException;
    private TimeSpan _startDelay = TimeSpan.Zero;

    /// <summary>Gets a value indicating whether <see cref="StartAsync" /> has been called.</summary>
    public bool StartCalled { get; private set; }

    /// <summary>Gets a value indicating whether <see cref="StopAsync" /> has been called.</summary>
    public bool StopCalled { get; private set; }

    /// <summary>Gets the number of times <see cref="StartAsync" /> has been called.</summary>
    public int StartCallCount { get; private set; }

    /// <summary>Gets the number of times <see cref="StopAsync" /> has been called.</summary>
    public int StopCallCount { get; private set; }

    /// <inheritdoc />
    public bool IsListening { get; private set; }

    /// <inheritdoc />
    public int? BoundPort => IsListening ? _boundPort : null;

    /// <summary>
    ///     Configures the stub to simulate a successful start on the given port.
    ///     Default: 8080.
    /// </summary>
    public StubProxyListener WithBoundPort(int port)
    {
        _boundPort = port;
        return this;
    }

    /// <summary>Configures the stub to throw the given exception when <see cref="StartAsync" /> is called.</summary>
    public StubProxyListener WithStartException(Exception exception)
    {
        _startException = exception;
        return this;
    }

    /// <summary>Configures the stub to simulate a delay before completing <see cref="StartAsync" />.</summary>
    public StubProxyListener WithStartDelay(TimeSpan delay)
    {
        _startDelay = delay;
        return this;
    }

    /// <inheritdoc />
    public async Task StartAsync(
        Func<IProxyConnection, CancellationToken, Task> onConnectionAccepted,
        CancellationToken cancellationToken)
    {
        StartCalled = true;
        StartCallCount++;

        if (_startDelay > TimeSpan.Zero)
        {
            await Task.Delay(_startDelay, cancellationToken).ConfigureAwait(false);
        }

        if (_startException is not null)
        {
            throw _startException;
        }

        _boundPort ??= 8080;
        IsListening = true;
    }

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken)
    {
        StopCalled = true;
        StopCallCount++;
        IsListening = false;
        return Task.CompletedTask;
    }
}
