using System.Threading.Tasks;

namespace Proxyfan.Framework.Networking.Tests;

/// <summary>
///     Tests for the <see cref="HypertextTransferProtocolProxyHandlerDependencies" /> parameter
///     object's optional fields.
/// </summary>
public sealed class HypertextTransferProtocolProxyHandlerDependenciesTests
{
    /// <summary>
    ///     Verifies that the dependencies object exposes optional upstream and throttle
    ///     fields with default null values.
    /// </summary>
    [Test]
    public async Task Construction_OptionalFields_DefaultToNull()
    {
        var dependencies = new HypertextTransferProtocolProxyHandlerDependencies
        {
            TrafficStore = null!,
            EventBus = null!,
            RuleEngine = null!,
            Logger = null!,
        };

        await Assert.That(dependencies.UpstreamProxy).IsNull();
        await Assert.That(dependencies.ThrottleProfile).IsNull();
    }
}
