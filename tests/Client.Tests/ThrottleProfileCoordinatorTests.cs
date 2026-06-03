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
    /// <summary>
    ///     Verifies that <see cref="ThrottleProfileCoordinator.Profile" /> reflects
    ///     the initial state of the underlying holder.
    /// </summary>
    [Test]
    public async Task Profile_SeededHolder_ReturnsInitialProfile()
    {
        var holder = new MutableThrottleProfile(ThrottleProfilePresets.ThirdGeneration());
        using var coordinator = new ThrottleProfileCoordinator(holder, InlineUserInterfaceScheduler.Instance);

        await Assert.That(coordinator.Profile).IsNotNull();
        await Assert.That(coordinator.Profile!.Name).IsEqualTo("3G");
    }

    /// <summary>
    ///     Verifies that <see cref="ThrottleProfileCoordinator.Profile" /> returns
    ///     <see langword="null" /> when the holder has no active profile.
    /// </summary>
    [Test]
    public async Task Profile_HolderDisabled_ReturnsNull()
    {
        var holder = new MutableThrottleProfile();
        using var coordinator = new ThrottleProfileCoordinator(holder, InlineUserInterfaceScheduler.Instance);

        await Assert.That(coordinator.Profile).IsNull();
    }

    /// <summary>
    ///     Verifies that <see cref="ThrottleProfileCoordinator.Apply" /> with a non-null
    ///     profile activates that profile on the underlying holder.
    /// </summary>
    [Test]
    public async Task Apply_NonNullProfile_SetsProfileOnHolder()
    {
        var holder = new MutableThrottleProfile();
        using var coordinator = new ThrottleProfileCoordinator(holder, InlineUserInterfaceScheduler.Instance);
        var profile = ThrottleProfilePresets.FastFourthGeneration();

        coordinator.Apply(profile);

        await Assert.That(holder.Profile).IsNotNull();
        await Assert.That(holder.Profile!.Name).IsEqualTo("4G");
    }

    /// <summary>
    ///     Verifies that <see cref="ThrottleProfileCoordinator.Apply" /> with
    ///     <see langword="null" /> disables throttling on the underlying holder.
    /// </summary>
    [Test]
    public async Task Apply_NullProfile_DisablesHolder()
    {
        var holder = new MutableThrottleProfile(ThrottleProfilePresets.Wireless());
        using var coordinator = new ThrottleProfileCoordinator(holder, InlineUserInterfaceScheduler.Instance);

        coordinator.Apply(null);

        await Assert.That(holder.Profile).IsNull();
    }

    /// <summary>
    ///     Verifies that an external change to the holder raises the coordinator's
    ///     <see cref="IThrottleProfileCoordinator.Changed" /> event with the new profile.
    /// </summary>
    [Test]
    public async Task Changed_ExternalProfileChange_RaisedWithNewProfile()
    {
        var holder = new MutableThrottleProfile();
        using var coordinator = new ThrottleProfileCoordinator(holder, InlineUserInterfaceScheduler.Instance);
        ThrottleProfile? received = null;
        coordinator.Changed += p => received = p;

        holder.SetProfile(ThrottleProfilePresets.SlowSecondGeneration());

        await Assert.That(received).IsNotNull();
        await Assert.That(received!.Name).IsEqualTo("2G");
    }

    /// <summary>
    ///     Verifies that an external disable on the holder raises the coordinator's
    ///     <see cref="IThrottleProfileCoordinator.Changed" /> event with <see langword="null" />.
    /// </summary>
    [Test]
    public async Task Changed_ExternalDisable_RaisedWithNull()
    {
        var holder = new MutableThrottleProfile(ThrottleProfilePresets.Wireless());
        using var coordinator = new ThrottleProfileCoordinator(holder, InlineUserInterfaceScheduler.Instance);
        var eventRaised = false;
        ThrottleProfile? received = ThrottleProfilePresets.Wireless();
        coordinator.Changed += p =>
        {
            eventRaised = true;
            received = p;
        };

        holder.Disable();

        await Assert.That(eventRaised).IsTrue();
        await Assert.That(received).IsNull();
    }

    /// <summary>
    ///     Verifies that after <see cref="ThrottleProfileCoordinator.Dispose" />, external
    ///     changes to the holder no longer raise the coordinator's Changed event.
    /// </summary>
    /// <remarks>
    ///     <c>using</c> is intentionally omitted: the coordinator is disposed explicitly before
    ///     the act step to verify post-disposal behaviour, so there is no dangling resource.
    /// </remarks>
    [Test]
    public async Task Dispose_AfterExternalChange_StopsForwardingEvents()
    {
        var holder = new MutableThrottleProfile();
        var coordinator = new ThrottleProfileCoordinator(holder, InlineUserInterfaceScheduler.Instance);
        var eventCount = 0;
        coordinator.Changed += _ => eventCount++;
        coordinator.Dispose();

        holder.SetProfile(ThrottleProfilePresets.ThirdGeneration());

        await Assert.That(eventCount).IsEqualTo(0);
    }
}
