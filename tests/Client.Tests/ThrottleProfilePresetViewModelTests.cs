using Proxyfan.Client.Tools.ViewModels;
using Proxyfan.Domain.Throttling;
using System.Threading.Tasks;
using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.Core;

namespace Proxyfan.Client.Tests;

/// <summary>
///     Tests for <see cref="ThrottleProfilePresetViewModel" />.
/// </summary>
public sealed class ThrottleProfilePresetViewModelTests
{
    [Test]
    public async Task Constructor_NullProfile_RepresentsOff()
    {
        var viewModel = new ThrottleProfilePresetViewModel("Off", null);

        await Assert.That(viewModel.DisplayName).IsEqualTo("Off");
        await Assert.That(viewModel.Profile).IsNull();
    }

    [Test]
    public async Task Constructor_WithProfile_StoresReference()
    {
        var profile = ThrottleProfilePresets.Wireless();

        var viewModel = new ThrottleProfilePresetViewModel("WiFi", profile);

        await Assert.That(viewModel.DisplayName).IsEqualTo("WiFi");
        await Assert.That(viewModel.Profile).IsSameReferenceAs(profile);
    }
}
