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

    [Test]
    public async Task ToggleVariousCheckboxes_FreshPreferences_AllPersistAndRestore()
    {
        await using var app = ProxyfanApp.Launch();
        var shell = new ShellPage(app);

        using var preferences = shell.OpenToolWindow("File", "Preferences...", "Preferences");
        try
        {
            // Combine the two toggle round-trips into one window open. Running
            // each toggle in a separate test (each with its own MSIX install +
            // Preferences open) caused late-suite UIA handle exhaustion in
            // direct-exe mode after ~5 sequential Preferences opens — the
            // tool window kept opening but the next menu pop-up failed to
            // materialise its sub-items. Folding the toggles together keeps
            // the suite stable while still exercising both checkboxes.
            var startProxy = preferences.CheckBox("Start proxy automatically on launch");
            var startProxyInitial = startProxy.IsChecked == true;
            startProxy.Click();
            preferences.WaitUntil(
                () => (startProxy.IsChecked == true) != startProxyInitial,
                description: "Start-on-launch toggled to opposite state");
            startProxy.Click();
            preferences.WaitUntil(
                () => (startProxy.IsChecked == true) == startProxyInitial,
                description: "Start-on-launch restored to original state");

            var useUpstream = preferences.CheckBox("Use upstream proxy");
            var useUpstreamInitial = useUpstream.IsChecked == true;
            useUpstream.Click();
            preferences.WaitUntil(
                () => (useUpstream.IsChecked == true) != useUpstreamInitial,
                description: "Use-upstream-proxy toggled to opposite state");
            useUpstream.Click();
            preferences.WaitUntil(
                () => (useUpstream.IsChecked == true) == useUpstreamInitial,
                description: "Use-upstream-proxy restored to original state");
        }
        finally
        {
            preferences.Close();
        }

        await Task.CompletedTask;
    }

    [Test]
    public async Task TypeIntoUpstreamHost_AfterEnablingUpstream_PreservesTypedText()
    {
        await using var app = ProxyfanApp.Launch();
        var shell = new ShellPage(app);

        using var preferences = shell.OpenToolWindow("File", "Preferences...", "Preferences");
        try
        {
            preferences.CheckBox("Use upstream proxy").Click();

            var host = preferences.TextBoxByName("Upstream proxy host");
            host.Text = "corp-proxy.example.com";
            preferences.WaitUntil(
                () => string.Equals(host.Text, "corp-proxy.example.com", System.StringComparison.Ordinal),
                description: "upstream host populated");
        }
        finally
        {
            preferences.Close();
        }

        await Task.CompletedTask;
    }

    [Test]
    public async Task ClickRestoreDefaults_FreshPreferences_LeavesWindowResponsive()
    {
        await using var app = ProxyfanApp.Launch();
        var shell = new ShellPage(app);

        using var preferences = shell.OpenToolWindow("File", "Preferences...", "Preferences");
        try
        {
            preferences.Button("Restore defaults").Click();

            await Assert.That(preferences.GetTitle()).IsEqualTo("Preferences");
            await Assert.That(preferences.HasButton("Apply")).IsTrue();
        }
        finally
        {
            preferences.Close();
        }
    }
}
