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
    public async Task Changed_WhenHolderProfileChanges_RaisesChangedEvent()
    {
        var holder = new MutableThrottleProfile();
        using var coordinator = new ThrottleProfileCoordinator(holder, InlineUserInterfaceScheduler.Instance);
        ThrottleProfile? observed = null;
        coordinator.Changed += profile => observed = profile;

        holder.SetProfile(ThrottleProfilePresets.SlowSecondGeneration());

        await Assert.That(observed).IsNotNull();
        await Assert.That(observed!.Name).IsEqualTo("2G");
    }

    [Test]
    public async Task Dispose_AfterExternalChange_DoesNotRaiseChanged()
    {
        var holder = new MutableThrottleProfile();
        var coordinator = new ThrottleProfileCoordinator(holder, InlineUserInterfaceScheduler.Instance);
        var notified = false;
        coordinator.Changed += _ => notified = true;
        coordinator.Dispose();

        holder.SetProfile(ThrottleProfilePresets.Wireless());

        await Assert.That(notified).IsFalse();
    }
}
