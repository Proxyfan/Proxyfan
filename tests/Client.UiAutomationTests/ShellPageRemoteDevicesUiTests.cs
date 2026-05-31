using Proxyfan.Client.UiAutomationTests.Infrastructure;
using System.Threading.Tasks;
using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.Core;

namespace Proxyfan.Client.UiAutomationTests;

/// <summary>
///     End-to-end FlaUI tests for the Remote Devices tool window opened from
///     <c>Tools → Remote Devices...</c>.
/// </summary>
public sealed class ShellPageRemoteDevicesUiTests : UiAutomationTestBase
{
    [Test]
    public async Task OpenRemoteDevices_FromToolsMenu_ShowsRemoteDevicesWindow()
    {
        await using var app = ProxyfanApp.Launch();
        var shell = new ShellPage(app);

        using var devices = shell.OpenToolWindow("Tools", "Remote Devices...", "Remote Devices");
        try
        {
            await Assert.That(devices.GetTitle()).IsEqualTo("Remote Devices");
            await Assert.That(devices.HasButton("Rename")).IsTrue();
            await Assert.That(devices.HasButton("Disconnect")).IsTrue();
            await Assert.That(devices.HasButton("Forget")).IsTrue();
        }
        finally
        {
            devices.Close();
        }
    }

    [Test]
    public async Task OpenRemoteDevices_FreshWindow_ExposesRenameTextBox()
    {
        await using var app = ProxyfanApp.Launch();
        var shell = new ShellPage(app);

        using var devices = shell.OpenToolWindow("Tools", "Remote Devices...", "Remote Devices");
        try
        {
            await Assert.That(devices.TextBoxByName("Device display name")).IsNotNull();
        }
        finally
        {
            devices.Close();
        }
    }
}
