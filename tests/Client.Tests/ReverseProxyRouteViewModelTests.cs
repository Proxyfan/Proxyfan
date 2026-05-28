using Proxyfan.Client.Tools.ViewModels;
using Proxyfan.Domain.Proxy;
using System.Threading.Tasks;
using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.Core;

namespace Proxyfan.Client.Tests;

/// <summary>
///     Tests for <see cref="ReverseProxyRouteViewModel" />.
/// </summary>
public sealed class ReverseProxyRouteViewModelTests
{
    [Test]
    public async Task Constructor_FromRoute_FormatsEndpointStringsAndProxiesProperties()
    {
        var route = new ReverseProxyRoute(
            identifier: "r1",
            name: "Test",
            listenPort: 9000,
            backendHost: "api.example",
            backendPort: 8443,
            transportLayerSecurityMode: ReverseProxyTransportLayerSecurityMode.Passthrough);

        var viewModel = new ReverseProxyRouteViewModel(route, ReverseProxyRouteStatus.Healthy);

        await Assert.That(viewModel.Route).IsSameReferenceAs(route);
        await Assert.That(viewModel.Identifier).IsEqualTo("r1");
        await Assert.That(viewModel.Name).IsEqualTo("Test");
        await Assert.That(viewModel.ListenEndPoint).IsEqualTo("127.0.0.1:9000");
        await Assert.That(viewModel.BackendEndPoint).IsEqualTo("api.example:8443");
        await Assert.That(viewModel.TransportLayerSecurityMode).IsEqualTo(ReverseProxyTransportLayerSecurityMode.Passthrough);
        await Assert.That(viewModel.Status).IsEqualTo(ReverseProxyRouteStatus.Healthy);
    }

    [Test]
    public async Task Status_OnAssignment_PropagatesChange()
    {
        var route = new ReverseProxyRoute(
            identifier: "r2",
            name: "Other",
            listenPort: 1234,
            backendHost: "x",
            backendPort: 80,
            transportLayerSecurityMode: ReverseProxyTransportLayerSecurityMode.None);
        var viewModel = new ReverseProxyRouteViewModel(route, ReverseProxyRouteStatus.Healthy);

        viewModel.Status = ReverseProxyRouteStatus.Stopped;

        await Assert.That(viewModel.Status).IsEqualTo(ReverseProxyRouteStatus.Stopped);
    }
}
