using Proxyfan.Client.Tools.ViewModels;
using Proxyfan.Domain.Throttling;
using System.Threading.Tasks;
using TUnit.Assertions;
using TUnit.Assertions.Extensions;

namespace Proxyfan.Client.Tests;

/// <summary>
///     Tests for <see cref="ThrottleCoordinator" />.
/// </summary>
public sealed class ThrottleCoordinatorTests
{
    [Test]
    public async Task Apply_OffIdentifier_DisablesHolder()
    {
        var holder = new MutableThrottleProfile(ThrottleProfilePresets.Wireless());
        using var coordinator = new ThrottleCoordinator(holder);
        IThrottleCoordinator abstraction = coordinator;

        abstraction.Apply(ThrottlePresetDefinitions.OffIdentifier);

        await Assert.That(holder.Profile).IsNull();
        await Assert.That(abstraction.ActiveProfileIdentifier).IsNull();
    }

    [Test]
    public async Task Apply_ThirdGenerationIdentifier_SetsHolderProfile()
    {
        var holder = new MutableThrottleProfile();
        using var coordinator = new ThrottleCoordinator(holder);
        IThrottleCoordinator abstraction = coordinator;

        abstraction.Apply("3G");

        await Assert.That(holder.Profile).IsNotNull();
        await Assert.That(holder.Profile!.Name).IsEqualTo("3G");
        await Assert.That(abstraction.ActiveProfileIdentifier).IsEqualTo("3G");
    }

    [Test]
    public async Task ProfileChanged_HolderProfileUpdated_RaisesIdentifierEvent()
    {
        var holder = new MutableThrottleProfile();
        using var coordinator = new ThrottleCoordinator(holder);
        IThrottleCoordinator abstraction = coordinator;
        string? observedIdentifier = null;
        abstraction.ProfileChanged += identifier => observedIdentifier = identifier;

        holder.SetProfile(ThrottleProfilePresets.BadNetwork());

        await Assert.That(observedIdentifier).IsEqualTo("Bad Network");
    }
}
