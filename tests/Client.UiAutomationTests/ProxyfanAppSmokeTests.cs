using Proxyfan.Client.UiAutomationTests.Infrastructure;
using System.Threading.Tasks;
using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.Core;

namespace Proxyfan.Client.UiAutomationTests;

/// <summary>
///     Smoke test that proves the FlaUI infrastructure is healthy: it can launch
///     <c>Client.Desktop.exe</c>, locate the main window via Windows UI Automation,
///     read its title, and tear the process down cleanly. If this test fails the
///     rest of the suite is meaningless, so it lives in a dedicated file named
///     after the type under test (<see cref="ProxyfanApp" />).
/// </summary>
public sealed class ProxyfanAppSmokeTests : UiAutomationTestBase
{
    [Test]
    public async Task Launch_FreshProcess_PresentsShellWindowWithProxyfanTitle()
    {
        await using var app = ProxyfanApp.Launch();

        var window = app.GetMainWindow();
        var title = window.Title;
        await Assert.That(title).IsEqualTo("Proxyfan");
        await Assert.That(window.Properties.IsOffscreen.Value).IsFalse();
    }
}
