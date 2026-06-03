using Proxyfan.Client.Tests.Stubs;
using Proxyfan.Client.Tools.ViewModels;
using Proxyfan.Domain.Throttling;
using System.Threading.Tasks;

namespace Proxyfan.Client.Tests;

/// <summary>
///     Tests for <see cref="ThrottleProfileCoordinator" />.
/// </summary>
public sealed class ThrottleProfileCoordinatorTests
{
    [Test]
    public async Task Apply_NullProfile_DisablesThrottle()
    {
        var holder = new MutableThrottleProfile(ThrottleProfilePresets.ThirdGeneration());
        using var coordinator = new ThrottleProfileCoordinator(holder, InlineUserInterfaceScheduler.Instance);

        coordinator.Apply(profile: null);

        await Assert.That(holder.Profile).IsNull();
    }

    [Test]
    public async Task Apply_Profile_SetsThrottle()
    {
        var holder = new MutableThrottleProfile();
        using var coordinator = new ThrottleProfileCoordinator(holder, InlineUserInterfaceScheduler.Instance);
        var profile = ThrottleProfilePresets.BadNetwork();

        coordinator.Apply(profile);

        await Assert.That(holder.Profile).IsNotNull();
        await Assert.That(holder.Profile!.Name).IsEqualTo(profile.Name);
    }

    [Test]
    public async Task HolderChange_ProfileRaised_EmitsChangedEvent()
    {
        var holder = new MutableThrottleProfile();
        using var coordinator = new ThrottleProfileCoordinator(holder, InlineUserInterfaceScheduler.Instance);
        ThrottleProfile? changedProfile = null;
        coordinator.Changed += profile => changedProfile = profile;
        var profile = ThrottleProfilePresets.FastFourthGeneration();

        holder.SetProfile(profile);

        await Assert.That(changedProfile).IsNotNull();
        await Assert.That(changedProfile!.Name).IsEqualTo(profile.Name);
    }
}
