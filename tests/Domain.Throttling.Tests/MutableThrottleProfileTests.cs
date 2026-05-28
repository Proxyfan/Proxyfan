using System.Threading.Tasks;

namespace Proxyfan.Domain.Throttling.Tests;

/// <summary>
///     Tests for <see cref="MutableThrottleProfile" />.
/// </summary>
public sealed class MutableThrottleProfileTests
{
    /// <summary>
    ///     Verifies that the default constructor starts with no active profile.
    /// </summary>
    [Test]
    public async Task DefaultConstructor_NoArguments_StartsWithNullProfile()
    {
        var holder = new MutableThrottleProfile();

        await Assert.That(holder.Profile).IsNull();
    }

    /// <summary>
    ///     Verifies that the seeded constructor accepts an initial profile.
    /// </summary>
    [Test]
    public async Task SeededConstructor_NonNullProfile_StartsWithProfile()
    {
        var initial = CreateProfile("p1");

        var holder = new MutableThrottleProfile(initial);

        await Assert.That(holder.Profile).IsSameReferenceAs(initial);
    }

    /// <summary>
    ///     Verifies that the seeded constructor accepts <see langword="null" /> as a starting state.
    /// </summary>
    [Test]
    public async Task SeededConstructor_NullProfile_StartsDisabled()
    {
        var holder = new MutableThrottleProfile(null);

        await Assert.That(holder.Profile).IsNull();
    }

    /// <summary>
    ///     Verifies that <see cref="MutableThrottleProfile.SetProfile" /> replaces the active profile
    ///     and fires <see cref="MutableThrottleProfile.Changed" />.
    /// </summary>
    [Test]
    public async Task SetProfile_NewProfile_StoresAndRaisesChanged()
    {
        var holder = new MutableThrottleProfile();
        var profile = CreateProfile("p2");
        ThrottleProfile? observed = null;
        var raisedCount = 0;
        holder.Changed += (_, p) =>
        {
            observed = p;
            raisedCount++;
        };

        holder.SetProfile(profile);

        await Assert.That(holder.Profile).IsSameReferenceAs(profile);
        await Assert.That(observed).IsSameReferenceAs(profile);
        await Assert.That(raisedCount).IsEqualTo(1);
    }

    /// <summary>
    ///     Verifies that setting the same reference is a no-op and does not raise Changed.
    /// </summary>
    [Test]
    public async Task SetProfile_SameReference_DoesNotRaiseChanged()
    {
        var profile = CreateProfile("p3");
        var holder = new MutableThrottleProfile(profile);
        var raisedCount = 0;
        holder.Changed += (_, _) => raisedCount++;

        holder.SetProfile(profile);

        await Assert.That(raisedCount).IsEqualTo(0);
    }

    /// <summary>
    ///     Verifies that <see cref="MutableThrottleProfile.Disable" /> clears the active profile
    ///     and raises Changed when a profile was set.
    /// </summary>
    [Test]
    public async Task Disable_ActiveProfile_ClearsAndRaisesChanged()
    {
        var holder = new MutableThrottleProfile(CreateProfile("p4"));
        ThrottleProfile? observed = CreateProfile("ignored");
        var raisedCount = 0;
        holder.Changed += (_, p) =>
        {
            observed = p;
            raisedCount++;
        };

        holder.Disable();

        await Assert.That(holder.Profile).IsNull();
        await Assert.That(observed).IsNull();
        await Assert.That(raisedCount).IsEqualTo(1);
    }

    /// <summary>
    ///     Verifies that <see cref="MutableThrottleProfile.Disable" /> is a no-op when already disabled.
    /// </summary>
    [Test]
    public async Task Disable_AlreadyDisabled_DoesNotRaiseChanged()
    {
        var holder = new MutableThrottleProfile();
        var raisedCount = 0;
        holder.Changed += (_, _) => raisedCount++;

        holder.Disable();

        await Assert.That(raisedCount).IsEqualTo(0);
    }

    /// <summary>
    ///     Verifies that disabling an active profile with no subscriber attached is safe and
    ///     still clears the profile. Exercises the null-conditional branch of the Changed event.
    /// </summary>
    [Test]
    public async Task Disable_ActiveProfileNoSubscriber_ClearsProfile()
    {
        var holder = new MutableThrottleProfile(CreateProfile("p5"));

        holder.Disable();

        await Assert.That(holder.Profile).IsNull();
    }

    /// <summary>
    ///     Verifies that setting a profile when no subscriber is attached is safe and still
    ///     stores the new reference. Exercises the null-conditional branch of the Changed event.
    /// </summary>
    [Test]
    public async Task SetProfile_NewProfileNoSubscriber_StoresProfile()
    {
        var holder = new MutableThrottleProfile();
        var profile = CreateProfile("p6");

        holder.SetProfile(profile);

        await Assert.That(holder.Profile).IsSameReferenceAs(profile);
    }

    private static ThrottleProfile CreateProfile(string name)
    {
        var parameters = new ThrottleProfileParameters
        {
            UploadBytesPerSecond = 1024,
            DownloadBytesPerSecond = 2048,
            Latency = System.TimeSpan.FromMilliseconds(5),
            PacketLossProbability = 0,
        };
        return new ThrottleProfile(name, parameters);
    }
}
