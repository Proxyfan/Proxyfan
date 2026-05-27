using Proxyfan.Domain.Certificates;
using System.Threading.Tasks;

namespace Proxyfan.Framework.Networking.Tests;

/// <summary>
///     Tests for <see cref="TransportLayerSecurityStrategySelector" />.
/// </summary>
public sealed class TransportLayerSecurityStrategySelectorTests
{
    /// <summary>
    ///     Verifies that a host matched by the SSL proxying list yields InterceptAndInspect.
    /// </summary>
    [Test]
    public async Task Select_ProxiedHost_ReturnsInterceptAndInspect()
    {
        var list = new ServerNameIndicationProxyingList(isEnabled: true);
        list.AddIncludedPattern("*");

        var strategy = TransportLayerSecurityStrategySelector.Select(list, "example.com");

        await Assert.That(strategy).IsEqualTo(TransportLayerSecurityHandlingStrategy.InterceptAndInspect);
    }

    /// <summary>
    ///     Verifies that an unmatched host yields PassThroughTunnel.
    /// </summary>
    [Test]
    public async Task Select_UnmatchedHost_ReturnsPassThroughTunnel()
    {
        var list = new ServerNameIndicationProxyingList(isEnabled: false);

        var strategy = TransportLayerSecurityStrategySelector.Select(list, "example.com");

        await Assert.That(strategy).IsEqualTo(TransportLayerSecurityHandlingStrategy.PassThroughTunnel);
    }

    /// <summary>
    ///     Verifies that an excluded host (on the list but explicitly excluded) yields PassThroughTunnel.
    /// </summary>
    [Test]
    public async Task Select_ExcludedHost_ReturnsPassThroughTunnel()
    {
        var list = new ServerNameIndicationProxyingList(isEnabled: true);
        list.AddIncludedPattern("*");
        list.AddExcludedPattern("excluded.example");

        var strategy = TransportLayerSecurityStrategySelector.Select(list, "excluded.example");

        await Assert.That(strategy).IsEqualTo(TransportLayerSecurityHandlingStrategy.PassThroughTunnel);
    }
}
