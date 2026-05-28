using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Proxyfan.Domain.Proxy.Events;
using Proxyfan.Domain.Proxy.Tests.Stubs;

namespace Proxyfan.Domain.Proxy.Tests;

/// <summary>
///     Additional behavior tests for <see cref="ProxyServer" /> covering recovery paths,
///     unexpected exceptions, and stop-when-faulted transitions.
/// </summary>
[NotInParallel]
public sealed class ProxyServerAdditionalTests
{
    /// <summary>
    ///     Verifies that <see cref="ProxyServer.StartAsync" /> returns a faulted result with a
    ///     <see cref="ProxyFaultedError" /> when the listener throws an unexpected exception.
    /// </summary>
    [Test]
    public async Task StartAsync_WhenListenerThrowsUnexpectedException_ReturnsFaultedError()
    {
        var listener = new StubProxyListener();
        listener.WithStartException(new InvalidOperationException("boom"));
        var bus = new StubDomainEventBus();
        var options = new StubOptionsMonitor<ProxyOptions>(new ProxyOptions { IsAutoStart = false });
        var logger = new StubLogger<ProxyServer>();
        var dispatcher = new StubConnectionDispatcher();
        await using var server = new ProxyServer(listener, dispatcher, options, bus, logger);

        var result = await server.StartAsync(CancellationToken.None);

        await Assert.That(result.IsSuccess).IsFalse();
        await Assert.That(result.Error).IsTypeOf<ProxyFaultedError>();
        await Assert.That(server.Status).IsEqualTo(ProxyStatus.Faulted);
    }

    /// <summary>
    ///     Verifies that <see cref="ProxyServer.StopAsync" /> on a faulted server transitions
    ///     to <see cref="ProxyStatus.Stopped" />.
    /// </summary>
    [Test]
    public async Task StopAsync_WhenFaulted_TransitionsToStopped()
    {
        var listener = new StubProxyListener();
        listener.WithStartException(new ProxyBindException(8080, new SocketException()));
        var bus = new StubDomainEventBus();
        var options = new StubOptionsMonitor<ProxyOptions>(new ProxyOptions { IsAutoStart = false });
        var logger = new StubLogger<ProxyServer>();
        var dispatcher = new StubConnectionDispatcher();
        await using var server = new ProxyServer(listener, dispatcher, options, bus, logger);
        await server.StartAsync(CancellationToken.None);
        await Assert.That(server.Status).IsEqualTo(ProxyStatus.Faulted);

        var result = await server.StopAsync(CancellationToken.None);

        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(server.Status).IsEqualTo(ProxyStatus.Stopped);
    }

    /// <summary>
    ///     Verifies that auto-start with a <see cref="ProxyBindException" /> transitions to faulted.
    /// </summary>
    [Test]
    public async Task AutoStart_WhenBindFails_TransitionsToFaulted()
    {
        var listener = new StubProxyListener();
        listener.WithStartException(new ProxyBindException(8080, new SocketException()));
        var bus = new StubDomainEventBus();
        var options = new StubOptionsMonitor<ProxyOptions>(new ProxyOptions { IsAutoStart = true });
        var logger = new StubLogger<ProxyServer>();
        var dispatcher = new StubConnectionDispatcher();
        await using var server = new ProxyServer(listener, dispatcher, options, bus, logger);

        await bus.WaitForPublishAsync<ProxyErrorOccurred>(CancellationToken.None);

        await Assert.That(server.Status).IsEqualTo(ProxyStatus.Faulted);
    }

    /// <summary>
    ///     Verifies that StartAsync falls back to <c>options.Port</c> when the listener
    ///     reports a null <c>BoundPort</c>.
    /// </summary>
    [Test]
    public async Task StartAsync_WhenListenerHasNullBoundPort_PublishesOptionsPort()
    {
        var listener = new StubProxyListener();
        listener.WithoutBoundPort();
        var bus = new StubDomainEventBus();
        var options = new StubOptionsMonitor<ProxyOptions>(new ProxyOptions { IsAutoStart = false, Port = 9090 });
        var logger = new StubLogger<ProxyServer>();
        var dispatcher = new StubConnectionDispatcher();
        await using var server = new ProxyServer(listener, dispatcher, options, bus, logger);

        var result = await server.StartAsync(CancellationToken.None);

        await Assert.That(result.IsSuccess).IsTrue();
        var startedEvent = await bus.WaitForPublishAsync<Proxyfan.Domain.Proxy.Events.ProxyStarted>(CancellationToken.None);
        await Assert.That(startedEvent.Port).IsEqualTo(9090);
    }

    /// <summary>
    ///     Verifies that RestartAsync from the Stopped state behaves like a plain StartAsync
    ///     (covering the false-branch of the "is Running or Faulted" check).
    /// </summary>
    [Test]
    public async Task RestartAsync_FromStopped_StartsTheServer()
    {
        var listener = new StubProxyListener();
        var bus = new StubDomainEventBus();
        var options = new StubOptionsMonitor<ProxyOptions>(new ProxyOptions { IsAutoStart = false });
        var logger = new StubLogger<ProxyServer>();
        var dispatcher = new StubConnectionDispatcher();
        await using var server = new ProxyServer(listener, dispatcher, options, bus, logger);

        var result = await server.RestartAsync(CancellationToken.None);

        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(server.Status).IsEqualTo(ProxyStatus.Running);
        await Assert.That(listener.StartCallCount).IsEqualTo(1);
        await Assert.That(listener.StopCallCount).IsEqualTo(0);
    }

    /// <summary>
    ///     Verifies that <see cref="ProxyServer.StopAsync" /> returns a faulted result with a
    ///     <see cref="ProxyFaultedError" /> when the listener throws an unexpected exception
    ///     during stop, transitioning the server to <see cref="ProxyStatus.Faulted" />.
    /// </summary>
    [Test]
    public async Task StopAsync_WhenListenerThrowsUnexpectedException_ReturnsFaultedError()
    {
        var listener = new StubProxyListener();
        listener.WithStopException(new InvalidOperationException("stop boom"));
        var bus = new StubDomainEventBus();
        var options = new StubOptionsMonitor<ProxyOptions>(new ProxyOptions { IsAutoStart = false });
        var logger = new StubLogger<ProxyServer>();
        var dispatcher = new StubConnectionDispatcher();
        await using var server = new ProxyServer(listener, dispatcher, options, bus, logger);
        await server.StartAsync(CancellationToken.None);
        await Assert.That(server.Status).IsEqualTo(ProxyStatus.Running);

        var result = await server.StopAsync(CancellationToken.None);

        await Assert.That(result.IsSuccess).IsFalse();
        await Assert.That(result.Error).IsTypeOf<ProxyFaultedError>();
        await Assert.That(server.Status).IsEqualTo(ProxyStatus.Faulted);
    }

    /// <summary>
    ///     Verifies that <see cref="ProxyServer.RestartAsync" /> short-circuits and returns the
    ///     stop failure (covering the !stopResult.IsSuccess branch in RestartAsync).
    /// </summary>
    [Test]
    public async Task RestartAsync_WhenStopFails_ReturnsStopFailureWithoutStarting()
    {
        var listener = new StubProxyListener();
        listener.WithStopException(new InvalidOperationException("stop boom"));
        var bus = new StubDomainEventBus();
        var options = new StubOptionsMonitor<ProxyOptions>(new ProxyOptions { IsAutoStart = false });
        var logger = new StubLogger<ProxyServer>();
        var dispatcher = new StubConnectionDispatcher();
        await using var server = new ProxyServer(listener, dispatcher, options, bus, logger);
        await server.StartAsync(CancellationToken.None);
        var startCallsBefore = listener.StartCallCount;

        var result = await server.RestartAsync(CancellationToken.None);

        await Assert.That(result.IsSuccess).IsFalse();
        await Assert.That(listener.StartCallCount).IsEqualTo(startCallsBefore);
    }

    /// <summary>
    ///     Verifies that incoming connections accepted by the listener are forwarded to the
    ///     configured <see cref="IConnectionDispatcher" /> (covers
    ///     <c>OnConnectionAcceptedAsync</c>).
    /// </summary>
    [Test]
    public async Task StartAsync_WhenListenerAcceptsConnection_DispatchesToConnectionDispatcher()
    {
        var listener = new StubProxyListener();
        var bus = new StubDomainEventBus();
        var options = new StubOptionsMonitor<ProxyOptions>(new ProxyOptions { IsAutoStart = false });
        var logger = new StubLogger<ProxyServer>();
        var dispatcher = new StubConnectionDispatcher();
        await using var server = new ProxyServer(listener, dispatcher, options, bus, logger);
        await server.StartAsync(CancellationToken.None);
        var connection = new StubProxyConnection();

        await listener.ConnectionAccepted!(connection, CancellationToken.None);

        await Assert.That(dispatcher.DispatchedConnections.Count).IsEqualTo(1);
        await Assert.That(dispatcher.DispatchedConnections[0]).IsSameReferenceAs(connection);
    }
}