using System;
using System.Threading.Tasks;

namespace Proxyfan.Domain.Proxy.Tests;

/// <summary>
///     Tests for <see cref="ReverseProxyRoute" />.
/// </summary>
public sealed class ReverseProxyRouteTests
{
    /// <summary>
    ///     Verifies the constructor populates properties.
    /// </summary>
    [Test]
    public async Task Constructor_ValidArguments_AssignsAllProperties()
    {
        var route = new ReverseProxyRoute(
            identifier: "api",
            name: "Internal API",
            listenPort: 9000,
            backendHost: "backend.local",
            backendPort: 8080,
            transportLayerSecurityMode: ReverseProxyTransportLayerSecurityMode.None);

        await Assert.That(route.Identifier).IsEqualTo("api");
        await Assert.That(route.Name).IsEqualTo("Internal API");
        await Assert.That(route.ListenPort).IsEqualTo(9000);
        await Assert.That(route.BackendHost).IsEqualTo("backend.local");
        await Assert.That(route.BackendPort).IsEqualTo(8080);
        await Assert.That(route.TransportLayerSecurityMode).IsEqualTo(ReverseProxyTransportLayerSecurityMode.None);
    }

    /// <summary>
    ///     Verifies an empty identifier throws.
    /// </summary>
    [Test]
    public async Task Constructor_EmptyIdentifier_Throws()
    {
        await Assert.That(() => new ReverseProxyRoute("", "n", 9000, "h", 80, ReverseProxyTransportLayerSecurityMode.None)).Throws<ArgumentException>();
    }

    /// <summary>
    ///     Verifies an empty name throws.
    /// </summary>
    [Test]
    public async Task Constructor_EmptyName_Throws()
    {
        await Assert.That(() => new ReverseProxyRoute("id", "", 9000, "h", 80, ReverseProxyTransportLayerSecurityMode.None)).Throws<ArgumentException>();
    }

    /// <summary>
    ///     Verifies an empty backend host throws.
    /// </summary>
    [Test]
    public async Task Constructor_EmptyBackendHost_Throws()
    {
        await Assert.That(() => new ReverseProxyRoute("id", "n", 9000, "", 80, ReverseProxyTransportLayerSecurityMode.None)).Throws<ArgumentException>();
    }

    /// <summary>
    ///     Verifies an out-of-range listen port throws.
    /// </summary>
    [Test]
    public async Task Constructor_ListenPortOutOfRange_Throws()
    {
        await Assert.That(() => new ReverseProxyRoute("id", "n", 0, "h", 80, ReverseProxyTransportLayerSecurityMode.None)).Throws<ArgumentOutOfRangeException>();
        await Assert.That(() => new ReverseProxyRoute("id", "n", 70000, "h", 80, ReverseProxyTransportLayerSecurityMode.None)).Throws<ArgumentOutOfRangeException>();
    }

    /// <summary>
    ///     Verifies an out-of-range backend port throws.
    /// </summary>
    [Test]
    public async Task Constructor_BackendPortOutOfRange_Throws()
    {
        await Assert.That(() => new ReverseProxyRoute("id", "n", 9000, "h", 0, ReverseProxyTransportLayerSecurityMode.None)).Throws<ArgumentOutOfRangeException>();
        await Assert.That(() => new ReverseProxyRoute("id", "n", 9000, "h", 70000, ReverseProxyTransportLayerSecurityMode.None)).Throws<ArgumentOutOfRangeException>();
    }
}
