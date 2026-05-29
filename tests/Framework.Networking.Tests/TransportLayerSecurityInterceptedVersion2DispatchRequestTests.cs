using Proxyfan.Framework.Networking.Tests.Stubs;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Proxyfan.Framework.Networking.Tests;

/// <summary>
///     Tests for <see cref="TransportLayerSecurityInterceptedVersion2DispatchRequest" />.
/// </summary>
public sealed class TransportLayerSecurityInterceptedVersion2DispatchRequestTests
{
    /// <summary>
    ///     Verifies that the required init properties round-trip the values supplied at
    ///     construction.
    /// </summary>
    [Test]
    public async Task InitProperties_AllAssigned_RoundTrip()
    {
        var bus = new StubDomainEventBus();
        var store = new StubTrafficStore();
        var connection = new StubProxyConnection();
        using var clientStream = new MemoryStream();
        using var serverStream = new MemoryStream();

        var request = new TransportLayerSecurityInterceptedVersion2DispatchRequest
        {
            ClientSecureStream = clientStream,
            Connection = connection,
            EventBus = bus,
            ServerSecureStream = serverStream,
            TrafficStore = store,
        };

        await Assert.That(request.ClientSecureStream).IsSameReferenceAs(clientStream);
        await Assert.That(request.Connection).IsSameReferenceAs(connection);
        await Assert.That(request.EventBus).IsSameReferenceAs(bus);
        await Assert.That(request.ServerSecureStream).IsSameReferenceAs(serverStream);
        await Assert.That(request.TrafficStore).IsSameReferenceAs(store);
    }
}
