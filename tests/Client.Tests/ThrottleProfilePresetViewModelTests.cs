using Proxyfan.Client.Tools.ViewModels;
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
    public async Task Constructor_IdentifierOnly_UsesIdentifierAsDisplayName()
    {
        var viewModel = new ThrottleProfilePresetViewModel("Off");

        await Assert.That(viewModel.DisplayName).IsEqualTo("Off");
        await Assert.That(viewModel.Identifier).IsEqualTo("Off");
    }

    [Test]
    public async Task Constructor_DisplayNameProvided_StoresDisplayName()
    {
        var viewModel = new ThrottleProfilePresetViewModel("WiFi", "Wireless");

        await Assert.That(viewModel.Identifier).IsEqualTo("WiFi");
        await Assert.That(viewModel.DisplayName).IsEqualTo("Wireless");
    }
}
