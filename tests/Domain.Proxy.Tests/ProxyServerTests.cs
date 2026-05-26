using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Proxyfan.Domain.Proxy.Events;
using Proxyfan.Domain.Proxy.Tests.Stubs;

namespace Proxyfan.Domain.Proxy.Tests;

/// <summary>
///     Tests for <see cref="ProxyServer" />.
/// </summary>
[NotInParallel]
public sealed class ProxyServerTests
{
    private StubDomainEventBus _eventBus;
    private StubProxyListener _listener;
    private StubOptionsMonitor<ProxyOptions>? _optionsMonitor;

    /// <summary>
    ///     Initializes a new instance of <see cref="ProxyServerTests" />.
    /// </summary>
    public ProxyServerTests()
    {
        _eventBus = null!;
        _listener = null!;
    }

    /// <summary>
    ///     Verifies that <see cref="ProxyServer.BoundPort" /> is null before starting.
    /// </summary>
    [Test]
    public async Task BoundPort_WhenStopped_IsNull()
    {
        await using var server = CreateServer();
        await Assert.That(server.BoundPort).IsNull();
    }

    /// <summary>
    ///     Stress test: concurrent start/stop calls must not corrupt state.
    /// </summary>
    [Test]
    public async Task ConcurrentStartStop_WhenInterleaved_DoesNotCorruptState()
    {
        await using var server = CreateServer();
        _listener.WithStartDelay(TimeSpan.FromMilliseconds(5));

        var tasks = new Task[20];

        for (var i = 0; i < 10; i++)
        {
            tasks[i] = Task.Run(() => server.StartAsync(CancellationToken.None));
            tasks[i + 10] = Task.Run(() => server.StopAsync(CancellationToken.None));
        }

        await Task.WhenAll(tasks);

        var finalStatus = server.Status;
        var isValidFinalState =
            finalStatus is ProxyStatus.Stopped
            or ProxyStatus.Running
            or ProxyStatus.Faulted;

        await Assert.That(isValidFinalState).IsTrue();
    }

    /// <summary>
    ///     Verifies that the server does not auto-start when <see cref="ProxyOptions.IsAutoStart" /> is false.
    /// </summary>
    [Test]
    public async Task Constructor_WhenAutoStartDisabled_RemainsStopped()
    {
        await using var server = CreateServer(new ProxyOptions { IsAutoStart = false });
        await Assert.That(server.Status).IsEqualTo(ProxyStatus.Stopped);
    }

    /// <summary>
    ///     Verifies that the server auto-starts when <see cref="ProxyOptions.IsAutoStart" /> is true.
    /// </summary>
    [Test]
    public async Task Constructor_WhenAutoStartEnabled_StartsAutomatically()
    {
        await using var server = CreateServer(new ProxyOptions { IsAutoStart = true });

        await _eventBus.WaitForPublishAsync<ProxyStarted>(CancellationToken.None);

        await Assert.That(server.Status).IsEqualTo(ProxyStatus.Running);
    }

    /// <summary>
    ///     Verifies that calling <see cref="ProxyServer.DisposeAsync" /> twice does not throw.
    /// </summary>
    [Test]
    public async Task DisposeAsync_CalledTwice_DoesNotThrow()
    {
        var server = CreateServer();

        await server.DisposeAsync();
        await Assert.That(async () => await server.DisposeAsync()).ThrowsNothing();
    }

    /// <summary>
    ///     Verifies that disposing a running server stops it.
    /// </summary>
    [Test]
    public async Task DisposeAsync_WhenRunning_StopsProxy()
    {
        var server = CreateServer();
        await server.StartAsync(CancellationToken.None);

        await server.DisposeAsync();

        await Assert.That(server.Status).IsEqualTo(ProxyStatus.Stopped);
        await Assert.That(_listener.StopCalled).IsTrue();
    }

    /// <summary>
    ///     Verifies that disposing a stopped server completes without error.
    /// </summary>
    [Test]
    public async Task DisposeAsync_WhenStopped_CompletesWithoutError()
    {
        var server = CreateServer();
        await Assert.That(async () => await server.DisposeAsync()).ThrowsNothing();
    }

    /// <summary>
    ///     Verifies that changing <see cref="ProxyOptions" /> while running triggers a restart.
    /// </summary>
    [Test]
    public async Task OptionsChange_WhenRunning_TriggersRestart()
    {
        await using var server = CreateServer();
        await server.StartAsync(CancellationToken.None);

        var waitForRestart = _eventBus.WaitForNextPublishAsync<ProxyStarted>(CancellationToken.None);
        _optionsMonitor!.RaiseChange(new ProxyOptions { Port = 9999, IsAutoStart = false });
        await waitForRestart;

        await Assert.That(server.Status).IsEqualTo(ProxyStatus.Running);
        await Assert.That(_listener.StartCallCount).IsEqualTo(2);
    }

    /// <summary>
    ///     Verifies that changing <see cref="ProxyOptions" /> while stopped triggers a restart (which just starts).
    /// </summary>
    [Test]
    public async Task OptionsChange_WhenStopped_TriggersRestart()
    {
        await using var server = CreateServer();

        var waitForStart = _eventBus.WaitForNextPublishAsync<ProxyStarted>(CancellationToken.None);
        _optionsMonitor!.RaiseChange(new ProxyOptions { Port = 9999, IsAutoStart = false });
        await waitForStart;

        await Assert.That(server.Status).IsEqualTo(ProxyStatus.Running);
    }

    /// <summary>
    ///     Verifies that <see cref="ProxyServer.RestartAsync" /> publishes both <see cref="ProxyStopped" />
    ///     and <see cref="ProxyStarted" /> events.
    /// </summary>
    [Test]
    public async Task RestartAsync_WhenRunning_PublishesBothEvents()
    {
        await using var server = CreateServer();
        await server.StartAsync(CancellationToken.None);

        await server.RestartAsync(CancellationToken.None);

        await Assert.That(_eventBus.PublishedOf<ProxyStopped>()).HasSingleItem();
        await Assert.That(_eventBus.PublishedOf<ProxyStarted>()).Count().IsEqualTo(2);
    }

    /// <summary>
    ///     Verifies that <see cref="ProxyServer.RestartAsync" /> stops and restarts atomically.
    /// </summary>
    [Test]
    public async Task RestartAsync_WhenRunning_StopsAndStartsAtomically()
    {
        await using var server = CreateServer();
        await server.StartAsync(CancellationToken.None);

        var result = await server.RestartAsync(CancellationToken.None);

        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(server.Status).IsEqualTo(ProxyStatus.Running);
        await Assert.That(_listener.StartCallCount).IsEqualTo(2);
        await Assert.That(_listener.StopCallCount).IsEqualTo(1);
    }

    /// <summary>
    ///     Verifies that restarting when stopped just starts the server without a stop call.
    /// </summary>
    [Test]
    public async Task RestartAsync_WhenStopped_JustStarts()
    {
        await using var server = CreateServer();

        var result = await server.RestartAsync(CancellationToken.None);

        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(server.Status).IsEqualTo(ProxyStatus.Running);
        await Assert.That(_listener.StopCallCount).IsEqualTo(0);
    }

    /// <summary>
    ///     Verifies that starting when already running is a no-op and returns success.
    /// </summary>
    [Test]
    public async Task StartAsync_WhenAlreadyRunning_ReturnsSuccessNoOp()
    {
        await using var server = CreateServer();
        await server.StartAsync(CancellationToken.None);

        var result = await server.StartAsync(CancellationToken.None);

        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(_listener.StartCallCount).IsEqualTo(1);
    }

    /// <summary>
    ///     Verifies that a bind failure error is a <see cref="ProxyBindError" />.
    /// </summary>
    [Test]
    public async Task StartAsync_WhenBindFails_ReturnsProxyBindError()
    {
        await using var server = CreateServer();
        _listener.WithStartException(new ProxyBindException(8080, new SocketException()));

        var result = await server.StartAsync(CancellationToken.None);

        await Assert.That(result.Error).IsTypeOf<ProxyBindError>();
    }

    /// <summary>
    ///     Verifies that a faulted server can recover by calling StartAsync again.
    /// </summary>
    [Test]
    public async Task StartAsync_WhenFaulted_CanRecover()
    {
        await using var server = CreateServer();
        _listener.WithStartException(new ProxyBindException(8080, new SocketException()));
        await server.StartAsync(CancellationToken.None);

        _listener.WithStartException(null!);
        var result = await server.StartAsync(CancellationToken.None);

        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(server.Status).IsEqualTo(ProxyStatus.Running);
    }

    /// <summary>
    ///     Verifies that a bind failure publishes <see cref="ProxyErrorOccurred" />.
    /// </summary>
    [Test]
    public async Task StartAsync_WhenListenerThrowsBindException_PublishesProxyErrorOccurredEvent()
    {
        await using var server = CreateServer();
        _listener.WithStartException(new ProxyBindException(8080, new SocketException()));

        await server.StartAsync(CancellationToken.None);

        await Assert.That(_eventBus.PublishedOf<ProxyErrorOccurred>()).HasSingleItem();
    }

    /// <summary>
    ///     Verifies that a bind failure transitions to Faulted and returns a failure result.
    /// </summary>
    [Test]
    public async Task StartAsync_WhenListenerThrowsBindException_TransitionsToFaulted()
    {
        await using var server = CreateServer();
        _listener.WithStartException(new ProxyBindException(8080, new SocketException()));

        var result = await server.StartAsync(CancellationToken.None);

        await Assert.That(result.IsSuccess).IsFalse();
        await Assert.That(server.Status).IsEqualTo(ProxyStatus.Faulted);
    }

    /// <summary>
    ///     Verifies that <see cref="ProxyStarted.Port" /> matches the bound port.
    /// </summary>
    [Test]
    public async Task StartAsync_WhenStarted_ProxyStartedEventHasCorrectPort()
    {
        await using var server = CreateServer();
        _listener.WithBoundPort(7777);

        await server.StartAsync(CancellationToken.None);

        var evt = System.Linq.Enumerable.First(_eventBus.PublishedOf<ProxyStarted>());
        await Assert.That(evt.Port).IsEqualTo(7777);
    }

    /// <summary>
    ///     Verifies that <see cref="ProxyStarted" /> is published when the proxy starts.
    /// </summary>
    [Test]
    public async Task StartAsync_WhenStopped_PublishesProxyStartedEvent()
    {
        await using var server = CreateServer();
        _listener.WithBoundPort(8080);

        await server.StartAsync(CancellationToken.None);

        var events = _eventBus.PublishedOf<ProxyStarted>();
        await Assert.That(events).HasSingleItem();
    }

    /// <summary>
    ///     Verifies that starting sets <see cref="ProxyServer.BoundPort" />.
    /// </summary>
    [Test]
    public async Task StartAsync_WhenStopped_SetsBoundPort()
    {
        await using var server = CreateServer();
        _listener.WithBoundPort(9090);

        await server.StartAsync(CancellationToken.None);

        await Assert.That(server.BoundPort).IsEqualTo(9090);
    }

    /// <summary>
    ///     Verifies that <see cref="ProxyServer.StartAsync" /> transitions status to Running.
    /// </summary>
    [Test]
    public async Task StartAsync_WhenStopped_TransitionsToRunning()
    {
        await using var server = CreateServer();

        var result = await server.StartAsync(CancellationToken.None);

        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(server.Status).IsEqualTo(ProxyStatus.Running);
    }

    /// <summary>
    ///     Verifies that a newly constructed server has <see cref="ProxyStatus.Stopped" /> status.
    /// </summary>
    [Test]
    public async Task Status_Initially_IsStopped()
    {
        await using var server = CreateServer();
        await Assert.That(server.Status).IsEqualTo(ProxyStatus.Stopped);
    }

    /// <summary>
    ///     Verifies that stopping when already stopped is a no-op and returns success.
    /// </summary>
    [Test]
    public async Task StopAsync_WhenAlreadyStopped_ReturnsSuccessNoOp()
    {
        await using var server = CreateServer();

        var result = await server.StopAsync(CancellationToken.None);

        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(_listener.StopCallCount).IsEqualTo(0);
    }

    /// <summary>
    ///     Verifies that <see cref="ProxyStopped" /> is published when the proxy stops.
    /// </summary>
    [Test]
    public async Task StopAsync_WhenRunning_PublishesProxyStoppedEvent()
    {
        await using var server = CreateServer();
        await server.StartAsync(CancellationToken.None);

        await server.StopAsync(CancellationToken.None);

        await Assert.That(_eventBus.PublishedOf<ProxyStopped>()).HasSingleItem();
    }

    /// <summary>
    ///     Verifies that <see cref="ProxyServer.StopAsync" /> transitions status to Stopped.
    /// </summary>
    [Test]
    public async Task StopAsync_WhenRunning_TransitionsToStopped()
    {
        await using var server = CreateServer();
        await server.StartAsync(CancellationToken.None);

        var result = await server.StopAsync(CancellationToken.None);

        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(server.Status).IsEqualTo(ProxyStatus.Stopped);
    }

    private ProxyServer CreateServer(ProxyOptions? options = null)
    {
        _listener = new StubProxyListener();
        _eventBus = new StubDomainEventBus();
        _optionsMonitor = new StubOptionsMonitor<ProxyOptions>(options ?? new ProxyOptions { IsAutoStart = false });
        var logger = new StubLogger<ProxyServer>();
        var dispatcher = new StubConnectionDispatcher();
        return new ProxyServer(_listener, dispatcher, _optionsMonitor, _eventBus, logger);
    }
}