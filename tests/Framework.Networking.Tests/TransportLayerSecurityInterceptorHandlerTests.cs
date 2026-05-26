using Microsoft.Extensions.Logging.Abstractions;
using Proxyfan.Domain;
using Proxyfan.Domain.Certificates;
using Proxyfan.Framework.Networking.Tests.Stubs;
using System;
using System.Buffers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Proxyfan.Framework.Networking.Tests;

/// <summary>
///     Tests for <see cref="TransportLayerSecurityInterceptorHandler" />.
/// </summary>
public sealed class TransportLayerSecurityInterceptorHandlerTests
{
    private static TransportLayerSecurityInterceptorHandler CreateHandler()
    {
        var proxyingList = new ServerNameIndicationProxyingList(isEnabled: true);
        var context = new TransportLayerSecurityInterceptionContext(new StubCertificateGenerator(), proxyingList);
        var trafficStore = new StubTrafficStore();
        var eventBus = new StubDomainEventBus();
        var logger = NullLogger<TransportLayerSecurityInterceptorHandler>.Instance;
        return new TransportLayerSecurityInterceptorHandler(context, trafficStore, eventBus, logger);
    }

    /// <summary>
    ///     Verifies that an exactly 8-byte CONNECT prefix returns true.
    /// </summary>
    [Test]
    public async Task CanHandle_ExactConnectPrefix_ReturnsTrue()
    {
        var handler = CreateHandler();
        var bytes = Encoding.ASCII.GetBytes("CONNECT ");
        var sequence = new ReadOnlySequence<byte>(bytes);

        var result = handler.CanHandle(sequence);

        await Assert.That(result).IsTrue();
    }

    /// <summary>
    ///     Verifies that a full CONNECT request line starting with CONNECT returns true.
    /// </summary>
    [Test]
    public async Task CanHandle_FullConnectRequest_ReturnsTrue()
    {
        var handler = CreateHandler();
        var bytes = Encoding.ASCII.GetBytes("CONNECT example.com:443 HTTP/1.1\r\n\r\n");
        var sequence = new ReadOnlySequence<byte>(bytes);

        var result = handler.CanHandle(sequence);

        await Assert.That(result).IsTrue();
    }

    /// <summary>
    ///     Verifies that a GET request is not handled by this handler.
    /// </summary>
    [Test]
    public async Task CanHandle_GetRequest_ReturnsFalse()
    {
        var handler = CreateHandler();
        var bytes = Encoding.ASCII.GetBytes("GET / HTTP/1.1\r\n\r\n");
        var sequence = new ReadOnlySequence<byte>(bytes);

        var result = handler.CanHandle(sequence);

        await Assert.That(result).IsFalse();
    }

    /// <summary>
    ///     Verifies that a buffer shorter than the CONNECT prefix returns false.
    /// </summary>
    [Test]
    public async Task CanHandle_ShortBuffer_ReturnsFalse()
    {
        var handler = CreateHandler();
        var bytes = Encoding.ASCII.GetBytes("CON");
        var sequence = new ReadOnlySequence<byte>(bytes);

        var result = handler.CanHandle(sequence);

        await Assert.That(result).IsFalse();
    }

    /// <summary>
    ///     Verifies that an empty buffer returns false.
    /// </summary>
    [Test]
    public async Task CanHandle_EmptyBuffer_ReturnsFalse()
    {
        var handler = CreateHandler();
        var sequence = ReadOnlySequence<byte>.Empty;

        var result = handler.CanHandle(sequence);

        await Assert.That(result).IsFalse();
    }
}