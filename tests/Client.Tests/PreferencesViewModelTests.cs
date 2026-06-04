using Proxyfan.Client.Tests.Stubs;
using Proxyfan.Client.Tools.ViewModels;
using Proxyfan.Domain.Configuration;
using Proxyfan.Presentation.Theming;
using System.Threading.Tasks;

namespace Proxyfan.Client.Tests;

/// <summary>
///     Tests for <see cref="PreferencesViewModel" /> covering load, apply, reset, and validation.
/// </summary>
public sealed class PreferencesViewModelTests
{
    /// <summary>
    ///     Verifies that the view model loads field values from the store at construction time.
    /// </summary>
    [Test]
    public async Task Construct_WithStoredPreferences_BindsFieldsFromStore()
    {
        var store = new StubUserPreferencesStore
        {
            PreferencesToLoad = new UserPreferences
            {
                CaptureMaximumFlows = 5000,
                IsRegisterSystemProxyOnStartup = false,
                IsStartProxyOnLaunch = false,
                IsUpstreamProxyEnabled = true,
                Locale = "ja-JP",
                LogLevel = "Trace",
                ProxyPort = 9000,
                Theme = "Dark",
                UpstreamProxyHost = "upstream.example.com",
                UpstreamProxyPort = 8081,
            },
        };
        var themeService = new ThemeService(AppTheme.System);

        var viewModel = new PreferencesViewModel(store, themeService);

        await Assert.That(viewModel.ProxyPort).IsEqualTo(9000);
        await Assert.That(viewModel.Locale).IsEqualTo("ja-JP");
        await Assert.That(viewModel.Theme).IsEqualTo("Dark");
        await Assert.That(viewModel.UpstreamProxyHost).IsEqualTo("upstream.example.com");
    }

    /// <summary>
    ///     Verifies that <see cref="PreferencesViewModel.ApplyCommand" /> persists the current
    ///     values to the store and updates the live theme.
    /// </summary>
    [Test]
    public async Task ApplyCommand_ValidValues_PersistsAndUpdatesTheme()
    {
        var store = new StubUserPreferencesStore();
        var themeService = new ThemeService(AppTheme.System);
        var viewModel = new PreferencesViewModel(store, themeService);

        viewModel.ProxyPort = 9090;
        viewModel.Theme = "Light";

        viewModel.ApplyCommand.Execute(parameter: null);

        await Assert.That(store.SaveCallCount).IsEqualTo(1);
        await Assert.That(store.LastSaved).IsNotNull();
        await Assert.That(store.LastSaved!.ProxyPort).IsEqualTo(9090);
        await Assert.That(themeService.CurrentTheme).IsEqualTo(AppTheme.Light);
        await Assert.That(viewModel.StatusMessage).IsEqualTo("Saved");
    }

    /// <summary>
    ///     Verifies that the apply command rejects an out-of-range proxy port without persisting.
    /// </summary>
    [Test]
    public async Task ApplyCommand_InvalidProxyPort_DoesNotPersist()
    {
        var store = new StubUserPreferencesStore();
        var themeService = new ThemeService(AppTheme.System);
        var viewModel = new PreferencesViewModel(store, themeService);

        viewModel.ProxyPort = 80;

        viewModel.ApplyCommand.Execute(parameter: null);

        await Assert.That(store.SaveCallCount).IsEqualTo(0);
        await Assert.That(viewModel.StatusMessage).IsEqualTo("Invalid values");
    }

    /// <summary>
    ///     Verifies that the apply command rejects an enabled upstream with a blank host.
    /// </summary>
    [Test]
    public async Task ApplyCommand_EnabledUpstreamWithoutHost_DoesNotPersist()
    {
        var store = new StubUserPreferencesStore();
        var themeService = new ThemeService(AppTheme.System);
        var viewModel = new PreferencesViewModel(store, themeService);

        viewModel.IsUpstreamProxyEnabled = true;
        viewModel.UpstreamProxyHost = "  ";
        viewModel.UpstreamProxyPort = 8080;

        viewModel.ApplyCommand.Execute(parameter: null);

        await Assert.That(store.SaveCallCount).IsEqualTo(0);
    }

    /// <summary>
    ///     Verifies that the reset command restores the defaults regardless of current values.
    /// </summary>
    [Test]
    public async Task ResetCommand_AnyState_RestoresDefaults()
    {
        var store = new StubUserPreferencesStore
        {
            PreferencesToLoad = new UserPreferences
            {
                CaptureMaximumFlows = 5000,
                IsRegisterSystemProxyOnStartup = false,
                IsStartProxyOnLaunch = false,
                IsUpstreamProxyEnabled = true,
                Locale = "fr-FR",
                LogLevel = "Trace",
                ProxyPort = 9999,
                Theme = "Dark",
                UpstreamProxyHost = "x.example.com",
                UpstreamProxyPort = 9090,
            },
        };
        var themeService = new ThemeService(AppTheme.System);
        var viewModel = new PreferencesViewModel(store, themeService);
        var defaults = UserPreferencesDefaults.Create();

        viewModel.ResetCommand.Execute(parameter: null);

        await Assert.That(viewModel.ProxyPort).IsEqualTo(defaults.ProxyPort);
        await Assert.That(viewModel.Theme).IsEqualTo(defaults.Theme);
        await Assert.That(viewModel.IsUpstreamProxyEnabled).IsEqualTo(defaults.IsUpstreamProxyEnabled);
    }

    /// <summary>
    ///     Verifies that applying with theme "System" sets the live theme to System.
    /// </summary>
    [Test]
    public async Task ApplyCommand_SystemTheme_SetsServiceToSystem()
    {
        var store = new StubUserPreferencesStore();
        var themeService = new ThemeService(AppTheme.Light);
        var viewModel = new PreferencesViewModel(store, themeService);

        viewModel.Theme = "System";
        viewModel.ApplyCommand.Execute(parameter: null);

        await Assert.That(themeService.CurrentTheme).IsEqualTo(AppTheme.System);
    }

    /// <summary>
    ///     Verifies the projected snapshot returned by <see cref="PreferencesViewModel.BuildPreferences" />
    ///     reflects all bound fields.
    /// </summary>
    [Test]
    public async Task BuildPreferences_FromCurrentValues_ReturnsMatchingSnapshot()
    {
        var store = new StubUserPreferencesStore();
        var themeService = new ThemeService(AppTheme.System);
        var viewModel = new PreferencesViewModel(store, themeService);

        viewModel.CaptureMaximumFlows = 1234;
        viewModel.LogLevel = "Warning";
        viewModel.UpstreamProxyPort = 3128;

        var snapshot = viewModel.BuildPreferences();

        await Assert.That(snapshot.CaptureMaximumFlows).IsEqualTo(1234);
        await Assert.That(snapshot.LogLevel).IsEqualTo("Warning");
        await Assert.That(snapshot.UpstreamProxyPort).IsEqualTo(3128);
    }

    /// <summary>
    ///     Verifies that a CaptureMaximumFlows value below 100 fails validation.
    /// </summary>
    [Test]
    public async Task ApplyCommand_CaptureMaximumFlowsBelowMinimum_DoesNotPersist()
    {
        var store = new StubUserPreferencesStore();
        var themeService = new ThemeService(AppTheme.System);
        var viewModel = new PreferencesViewModel(store, themeService) { CaptureMaximumFlows = 50 };

        viewModel.ApplyCommand.Execute(parameter: null);

        await Assert.That(store.SaveCallCount).IsEqualTo(0);
        await Assert.That(viewModel.StatusMessage).IsEqualTo("Invalid values");
    }

    /// <summary>
    ///     Verifies that a CaptureMaximumFlows value above the supported maximum fails validation.
    /// </summary>
    [Test]
    public async Task ApplyCommand_CaptureMaximumFlowsAboveMaximum_DoesNotPersist()
    {
        var store = new StubUserPreferencesStore();
        var themeService = new ThemeService(AppTheme.System);
        var viewModel = new PreferencesViewModel(store, themeService) { CaptureMaximumFlows = 1_000_001 };

        viewModel.ApplyCommand.Execute(parameter: null);

        await Assert.That(store.SaveCallCount).IsEqualTo(0);
        await Assert.That(viewModel.StatusMessage).IsEqualTo("Invalid values");
    }

    /// <summary>
    ///     Verifies that an out-of-range UpstreamProxyPort fails validation when upstream is enabled.
    /// </summary>
    [Test]
    public async Task ApplyCommand_EnabledUpstreamPortOutOfRange_DoesNotPersist()
    {
        var store = new StubUserPreferencesStore();
        var themeService = new ThemeService(AppTheme.System);
        var viewModel = new PreferencesViewModel(store, themeService)
        {
            IsUpstreamProxyEnabled = true,
            UpstreamProxyHost = "upstream.example.com",
            UpstreamProxyPort = 0,
        };

        viewModel.ApplyCommand.Execute(parameter: null);

        await Assert.That(store.SaveCallCount).IsEqualTo(0);
    }

    /// <summary>
    ///     Verifies that a port above 65535 fails validation.
    /// </summary>
    [Test]
    public async Task ApplyCommand_ProxyPortAboveMaximum_DoesNotPersist()
    {
        var store = new StubUserPreferencesStore();
        var themeService = new ThemeService(AppTheme.System);
        var viewModel = new PreferencesViewModel(store, themeService) { ProxyPort = 70000 };

        viewModel.ApplyCommand.Execute(parameter: null);

        await Assert.That(store.SaveCallCount).IsEqualTo(0);
    }

    /// <summary>
    ///     Verifies that a fully-valid upstream configuration is persisted.
    /// </summary>
    [Test]
    public async Task ApplyCommand_ValidUpstreamConfiguration_Persists()
    {
        var store = new StubUserPreferencesStore();
        var themeService = new ThemeService(AppTheme.System);
        var viewModel = new PreferencesViewModel(store, themeService)
        {
            IsUpstreamProxyEnabled = true,
            UpstreamProxyHost = "upstream.example.com",
            UpstreamProxyPort = 3128,
        };

        viewModel.ApplyCommand.Execute(parameter: null);

        await Assert.That(store.SaveCallCount).IsEqualTo(1);
        await Assert.That(store.LastSaved!.IsUpstreamProxyEnabled).IsTrue();
        await Assert.That(store.LastSaved.UpstreamProxyHost).IsEqualTo("upstream.example.com");
        await Assert.That(store.LastSaved.UpstreamProxyPort).IsEqualTo(3128);
    }
}
