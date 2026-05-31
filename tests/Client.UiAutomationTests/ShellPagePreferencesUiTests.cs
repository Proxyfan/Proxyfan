using Proxyfan.Client.UiAutomationTests.Infrastructure;
using System.Threading.Tasks;
using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.Core;

namespace Proxyfan.Client.UiAutomationTests;

/// <summary>
///     End-to-end FlaUI tests for the Preferences tool window opened from the
///     shell's File menu (<c>docs/DESIGN.md § 11 Configuration and
///     Preferences</c>). Each test launches the shell from a fresh MSIX
///     install, opens the Preferences window, and exercises its controls.
/// </summary>
public sealed class ShellPagePreferencesUiTests : UiAutomationTestBase
{
    [Test]
    public async Task OpenPreferences_FromFileMenu_ShowsPreferencesWindowWithExpectedTitle()
    {
        await using var app = ProxyfanApp.Launch();
        var shell = new ShellPage(app);

        using var preferences = shell.OpenToolWindow("File", "Preferences...", "Preferences");

        try
        {
            await Assert.That(preferences.GetTitle()).IsEqualTo("Preferences");
            await Assert.That(preferences.HasButton("Apply")).IsTrue();
            await Assert.That(preferences.HasButton("Restore defaults")).IsTrue();
        }
        finally
        {
            preferences.Close();
        }
    }

    [Test]
    public async Task OpenPreferences_FreshShell_ExposesLocaleAndThemeAndLogLevelControls()
    {
        await using var app = ProxyfanApp.Launch();
        var shell = new ShellPage(app);

        using var preferences = shell.OpenToolWindow("File", "Preferences...", "Preferences");

        try
        {
            await Assert.That(preferences.TextBoxByName("UI locale")).IsNotNull();
            await Assert.That(preferences.ComboBoxByName("Application theme")).IsNotNull();
            await Assert.That(preferences.ComboBoxByName("Log level")).IsNotNull();
        }
        finally
        {
            preferences.Close();
        }
    }

    [Test]
    public async Task TypeIntoLocale_FreshPreferences_PropagatesTypedTextIntoTextBox()
    {
        await using var app = ProxyfanApp.Launch();
        var shell = new ShellPage(app);

        using var preferences = shell.OpenToolWindow("File", "Preferences...", "Preferences");

        try
        {
            var locale = preferences.TextBoxByName("UI locale");
            locale.Focus();
            locale.Text = "en-US";

            preferences.WaitUntil(
                () => string.Equals(locale.Text, "en-US", System.StringComparison.Ordinal),
                description: "locale textbox updated to en-US");
        }
        finally
        {
            preferences.Close();
        }

        await Task.CompletedTask;
    }
}
