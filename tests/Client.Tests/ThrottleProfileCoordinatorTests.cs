using Proxyfan.Client.Tests.Stubs;
using Proxyfan.Client.Tools.ViewModels;
using Proxyfan.Domain.Throttling;
using System.Threading.Tasks;

namespace Proxyfan.Client.Tests;

/// <summary>
///     Tests for <see cref="ThrottleProfileCoordinator" />.
/// </summary>
[NotInParallel]
public sealed class ThrottleProfileCoordinatorTests
{
    [Test]
    public async Task Apply_Disable_ClearsProfile()
    {
        var holder = new MutableThrottleProfile(ThrottleProfilePresets.Wireless());
        using var coordinator = new ThrottleProfileCoordinator(holder, InlineUserInterfaceScheduler.Instance);

        coordinator.Apply(profile: null);

        await Assert.That(holder.Profile).IsNull();
    }

    [Test]
    public async Task Apply_Profile_SetsProfile()
    {
        var holder = new MutableThrottleProfile();
        using var coordinator = new ThrottleProfileCoordinator(holder, InlineUserInterfaceScheduler.Instance);
        var profile = ThrottleProfilePresets.ThirdGeneration();

        coordinator.Apply(profile);

        await Assert.That(holder.Profile).IsNotNull();
        await Assert.That(holder.Profile!.Name).IsEqualTo("3G");
    }

    [Test]
    public async Task Changed_HolderChanges_RaisesWithLatestProfile()
    {
        var holder = new MutableThrottleProfile();
        using var coordinator = new ThrottleProfileCoordinator(holder, InlineUserInterfaceScheduler.Instance);
        ThrottleProfile? raised = null;
        coordinator.Changed += profile => raised = profile;

        holder.SetProfile(ThrottleProfilePresets.FastFourthGeneration());

        await Assert.That(raised).IsNotNull();
        await Assert.That(raised!.Name).IsEqualTo("4G");
    }
}
