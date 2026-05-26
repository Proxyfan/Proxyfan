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
}