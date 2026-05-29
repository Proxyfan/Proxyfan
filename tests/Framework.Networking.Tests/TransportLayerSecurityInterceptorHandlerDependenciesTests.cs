using System.Threading.Tasks;

namespace Proxyfan.Framework.Networking.Tests;

/// <summary>
///     Tests for the new <see cref="TransportLayerSecurityInterceptorHandlerDependencies" />
///     parameter-object pattern.
/// </summary>
public sealed class TransportLayerSecurityInterceptorHandlerDependenciesTests
{
    /// <summary>
    ///     Verifies that the dependencies object exposes optional rule engine and breakpoint
    ///     handler fields with default null values.
    /// </summary>
    [Test]
    public async Task Construction_OptionalFields_DefaultToNull()
    {
        var dependencies = new TransportLayerSecurityInterceptorHandlerDependencies
        {
            Context = null!,
            EventBus = null!,
            Logger = null!,
            TrafficStore = null!,
        };

        await Assert.That(dependencies.RuleEngine).IsNull();
        await Assert.That(dependencies.BreakpointHandler).IsNull();
        await Assert.That(dependencies.ScriptingHandler).IsNull();
        await Assert.That(dependencies.HostResolver).IsNull();
        await Assert.That(dependencies.ServerSentEventsStore).IsNull();
        await Assert.That(dependencies.WebSocketStore).IsNull();
        await Assert.That(dependencies.ThrottleProfile).IsNull();
        await Assert.That(dependencies.PacketLossSampler).IsNull();
        await Assert.That(dependencies.TimeProvider).IsNull();
    }
}
