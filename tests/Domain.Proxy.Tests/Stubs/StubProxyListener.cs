using System;
using System.Threading;
using System.Threading.Tasks;

namespace Proxyfan.Domain.Proxy.Tests.Stubs;

/// <summary>
///     A configurable stub for <see cref="IProxyListener" /> used in unit tests.
/// </summary>
public sealed class StubProxyListener : IProxyListener
{
    private int? _boundPort;
    private bool _suppressDefaultBoundPort;
    private TimeSpan _startDelay;
    private Exception? _startException;
    private Exception? _stopException;
    private readonly TimeProvider _timeProvider;

    /// <summary>
    ///     Gets a value indicating whether <see cref="StartAsync" /> has been called at least once.
    /// </summary>
    public bool StartCalled { get; private set; }

    /// <summary>
    ///     Gets the total number of times <see cref="StartAsync" /> has been called.
    /// </summary>
    public int StartCallCount { get; private set; }

    /// <summary>
    ///     Gets a value indicating whether <see cref="StopAsync" /> has been called at least once.
    /// </summary>
    public bool StopCalled { get; private set; }

    /// <summary>
    ///     Gets the total number of times <see cref="StopAsync" /> has been called.
    /// </summary>
    public int StopCallCount { get; private set; }

    /// <inheritdoc />
    public bool IsListening { get; private set; }

    /// <inheritdoc />
    public int? BoundPort => IsListening ? _boundPort : null;

    /// <summary>
    ///     Initializes a new instance of <see cref="StubProxyListener" />.
    /// </summary>
    public StubProxyListener()
    {
        _startDelay = TimeSpan.Zero;
        _timeProvider = TimeProvider.System;
    }

    /// <inheritdoc />
    public async Task StartAsync(ConnectionAcceptedHandler onConnectionAccepted, CancellationToken cancellationToken)
    {
        StartCalled = true;
        StartCallCount++;
        ConnectionAccepted = onConnectionAccepted;

        if (_startDelay > TimeSpan.Zero)
        {
            await Task.Delay(_startDelay, _timeProvider, cancellationToken).ConfigureAwait(false);
        }

        if (_startException is not null)
        {
            throw _startException;
        }

        _boundPort ??= _suppressDefaultBoundPort ? null : 8080;
        IsListening = true;
    }

    /// <summary>
    ///     Gets the handler captured from the most recent <see cref="StartAsync" /> call.
    ///     Test code can invoke this to simulate an inbound connection.
    /// </summary>
    public ConnectionAcceptedHandler? ConnectionAccepted { get; private set; }

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken)
    {
        StopCalled = true;
        StopCallCount++;
        IsListening = false;

        if (_stopException is not null)
        {
            throw _stopException;
        }

        return Task.CompletedTask;
    }

    /// <summary>
    ///     Configures the stub to simulate a successful start on the given port.
    ///     Default is port 8080 when not configured.
    /// </summary>
    /// <param name="port">
    ///     The port number to report as the bound port after a successful start.
    /// </param>
    /// <returns>
    ///     This instance for fluent chaining.
    /// </returns>
    public StubProxyListener WithBoundPort(int port)
    {
        _boundPort = port;
        return this;
    }

    /// <summary>
    ///     Configures the stub to simulate a delay before completing <see cref="StartAsync" />.
    /// </summary>
    /// <param name="delay">
    ///     The delay to introduce before <see cref="StartAsync" /> completes.
    /// </param>
    /// <returns>
    ///     This instance for fluent chaining.
    /// </returns>
    public StubProxyListener WithStartDelay(TimeSpan delay)
    {
        _startDelay = delay;
        return this;
    }

    /// <summary>
    ///     Configures the stub to throw the given exception when <see cref="StartAsync" /> is called.
    /// </summary>
    /// <param name="exception">
    ///     The exception to throw, or <see langword="null" /> to clear a previously configured exception.
    /// </param>
    /// <returns>
    ///     This instance for fluent chaining.
    /// </returns>
    public StubProxyListener WithStartException(Exception exception)
    {
        _startException = exception;
        return this;
    }

    /// <summary>
    ///     Configures the stub to NOT default <see cref="BoundPort" /> to 8080 after
    ///     <see cref="StartAsync" /> succeeds. <see cref="BoundPort" /> remains null,
    ///     exercising the null-coalescing fallback in callers.
    /// </summary>
    /// <returns>This instance for fluent chaining.</returns>
    public StubProxyListener WithoutBoundPort()
    {
        _suppressDefaultBoundPort = true;
        return this;
    }

    /// <summary>
    ///     Configures the stub to throw the given exception when <see cref="StopAsync" /> is called.
    /// </summary>
    /// <param name="exception">The exception to throw.</param>
    /// <returns>This instance for fluent chaining.</returns>
    public StubProxyListener WithStopException(Exception exception)
    {
        _stopException = exception;
        return this;
    }
}