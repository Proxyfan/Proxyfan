using Proxyfan.Domain.Proxy;
using Proxyfan.Framework.Networking.Tests.Stubs;
using System.Threading.Tasks;

namespace Proxyfan.Framework.Networking.Tests;

/// <summary>
///     Tests for <see cref="ReverseProxyRouteListenerFactory" />.
/// </summary>
public sealed class ReverseProxyRouteListenerFactoryTests
{
    /// <summary>
    ///     Verifies the factory returns a listener bound to the supplied route.
    /// </summary>
    [Test]
    public async Task Create_ValidRoute_ReturnsListenerWithSameRoute()
    {
        var route = new ReverseProxyRoute(
            "x",
            "X",
            65503,
            "127.0.0.1",
            65500,
            ReverseProxyTransportLayerSecurityMode.None);
        using var factory = new StubLoggerFactory();

        using var listener = ReverseProxyRouteListenerFactory.Create(route, factory, hypertextTransferProtocolHandler: null);

        await Assert.That(listener.GetRoute()).IsSameReferenceAs(route);
    }
}
