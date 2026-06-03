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
        var coordinator = new ThrottleProfileCoordinator(holder, InlineUserInterfaceScheduler.Instance);

        coordinator.Apply(profile: null);

        await Assert.That(holder.Profile).IsNull();
    }

    [Test]
    public async Task Apply_ProfileProvided_SetsThrottle()
    {
        var holder = new MutableThrottleProfile();
        var coordinator = new ThrottleProfileCoordinator(holder, InlineUserInterfaceScheduler.Instance);
        var profile = ThrottleProfilePresets.Wireless();

        coordinator.Apply(profile);

        await Assert.That(holder.Profile).IsNotNull();
        await Assert.That(holder.Profile!.Name).IsEqualTo(profile.Name);
    }

    [Test]
    public async Task HolderChange_ProfileUpdated_RaisesChanged()
    {
        var holder = new MutableThrottleProfile();
        var coordinator = new ThrottleProfileCoordinator(holder, InlineUserInterfaceScheduler.Instance);
        ThrottleProfile? observedProfile = null;
        coordinator.Changed += profile =>
        {
            observedProfile = profile;
        };

        var profile = ThrottleProfilePresets.FastFourthGeneration();
        holder.SetProfile(profile);

        await Assert.That(observedProfile).IsNotNull();
        await Assert.That(observedProfile!.Name).IsEqualTo(profile.Name);
    }

}
