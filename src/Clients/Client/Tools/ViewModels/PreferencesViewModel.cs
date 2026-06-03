using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Proxyfan.Domain.Configuration;
using Proxyfan.Presentation.Theming;

namespace Proxyfan.Client.Tools.ViewModels;

/// <summary>
///     View model for the Preferences tool window. Lets the user edit the persisted
///     <see cref="UserPreferences" /> and apply them. Theme changes are applied immediately to
///     the live <see cref="ThemeService" />; proxy/network settings are written to disk and take
///     effect on the next application launch.
/// </summary>
public sealed partial class PreferencesViewModel : ObservableObject
{
    private const string StatusApplied = "Saved";
    private const string StatusInvalid = "Invalid values";
    private readonly IUserPreferencesStore _store;
    private readonly ThemeService _themeService;
    [ObservableProperty]
    private int _captureMaximumFlows;
    [ObservableProperty]
    private bool _isRegisterSystemProxyOnStartup;
    [ObservableProperty]
    private bool _isStartProxyOnLaunch;
    [ObservableProperty]
    private bool _isUpstreamProxyEnabled;
    [ObservableProperty]
    private string? _locale;
    [ObservableProperty]
    private string _logLevel;
    [ObservableProperty]
    private int _proxyPort;
    [ObservableProperty]
    private string _statusMessage;
    [ObservableProperty]
    private string _theme;
    [ObservableProperty]
    private string? _upstreamProxyHost;
    [ObservableProperty]
    private int _upstreamProxyPort;

    /// <summary>
    ///     Initializes a new <see cref="PreferencesViewModel" /> by loading the current
    ///     preferences from the supplied store.
    /// </summary>
    /// <param name="store">The store used to load and persist preferences.</param>
    /// <param name="themeService">The live theme service mutated when the theme changes.</param>
    public PreferencesViewModel(IUserPreferencesStore store, ThemeService themeService)
    {
        _store = store;
        _themeService = themeService;
        var loaded = store.Load();
        _captureMaximumFlows = loaded.CaptureMaximumFlows;
        _isRegisterSystemProxyOnStartup = loaded.IsRegisterSystemProxyOnStartup;
        _isStartProxyOnLaunch = loaded.IsStartProxyOnLaunch;
        _isUpstreamProxyEnabled = loaded.IsUpstreamProxyEnabled;
        _locale = loaded.Locale;
        _logLevel = loaded.LogLevel;
        _proxyPort = loaded.ProxyPort;
        _theme = loaded.Theme;
        _upstreamProxyHost = loaded.UpstreamProxyHost;
        _upstreamProxyPort = loaded.UpstreamProxyPort;
        _statusMessage = string.Empty;
    }

    /// <summary>
    ///     Builds a <see cref="UserPreferences" /> snapshot from the currently bound values.
    ///     Surfaced as a public method so tests can validate the projection.
    /// </summary>
    /// <returns>The composed preferences.</returns>
    public UserPreferences BuildPreferences()
    {
        var preferences = new UserPreferences
        {
            CaptureMaximumFlows = CaptureMaximumFlows,
            IsRegisterSystemProxyOnStartup = IsRegisterSystemProxyOnStartup,
            IsStartProxyOnLaunch = IsStartProxyOnLaunch,
            IsUpstreamProxyEnabled = IsUpstreamProxyEnabled,
            Locale = Locale,
            LogLevel = LogLevel,
            ProxyPort = ProxyPort,
            Theme = Theme,
            UpstreamProxyHost = UpstreamProxyHost,
            UpstreamProxyPort = UpstreamProxyPort,
        };
        return preferences;
    }

    /// <summary>
    ///     Returns <see langword="true" /> when the currently bound values are valid for save.
    /// </summary>
    /// <returns><see langword="true" /> when all values pass validation.</returns>
    public bool HasValidValues()
    {
        if (ProxyPort is < 1024 or > 65535)
        {
            return false;
        }

        if (!UserPreferencesValidation.HasValidCaptureMaximumFlows(CaptureMaximumFlows))
        {
            return false;
        }

        if (IsUpstreamProxyEnabled)
        {
            if (string.IsNullOrWhiteSpace(UpstreamProxyHost))
            {
                return false;
            }

            if (UpstreamProxyPort is < 1 or > 65535)
            {
                return false;
            }
        }

        return true;
    }

    [RelayCommand]
    private void Apply()
    {
        if (!HasValidValues())
        {
            StatusMessage = StatusInvalid;
            return;
        }

        var preferences = BuildPreferences();
        _store.Save(preferences);
        var theme = AppThemeParser.Parse(preferences.Theme);
        _themeService.SwitchTheme(theme);
        StatusMessage = StatusApplied;
    }

    [RelayCommand]
    private void Reset()
    {
        var defaults = UserPreferencesDefaults.Create();
        CaptureMaximumFlows = defaults.CaptureMaximumFlows;
        IsRegisterSystemProxyOnStartup = defaults.IsRegisterSystemProxyOnStartup;
        IsStartProxyOnLaunch = defaults.IsStartProxyOnLaunch;
        IsUpstreamProxyEnabled = defaults.IsUpstreamProxyEnabled;
        Locale = defaults.Locale;
        LogLevel = defaults.LogLevel;
        ProxyPort = defaults.ProxyPort;
        Theme = defaults.Theme;
        UpstreamProxyHost = defaults.UpstreamProxyHost;
        UpstreamProxyPort = defaults.UpstreamProxyPort;
        StatusMessage = string.Empty;
    }
}
