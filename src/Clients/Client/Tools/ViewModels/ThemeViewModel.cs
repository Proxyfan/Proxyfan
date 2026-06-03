using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Proxyfan.Presentation.Localization;
using Proxyfan.Presentation.Theming;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace Proxyfan.Client.Tools.ViewModels;

/// <summary>
///     View model for the Theme picker tool window. Lets the user choose between
///     System, Light, and Dark themes; the selection is applied through <see cref="ThemeService" />.
///     Display names are resolved through the supplied <see cref="LocalizationService" /> so the
///     picker reflects Proxyfan's locale resolution and runtime language switching.
/// </summary>
public sealed partial class ThemeViewModel : ObservableObject, IDisposable
{
    private const string DarkResourceKey = "Tools_Theme_Option_Dark";
    private const string LightResourceKey = "Tools_Theme_Option_Light";
    private const string SystemResourceKey = "Tools_Theme_Option_System";
    private readonly LocalizationService _localization;
    private readonly ThemeService _themeService;
    [ObservableProperty]
    private ThemeOptionViewModel? _selectedOption;

    /// <summary>
    ///     Gets the list of available theme options.
    /// </summary>
    public ObservableCollection<ThemeOptionViewModel> Options { get; }

    /// <summary>
    ///     Initializes a new <see cref="ThemeViewModel" /> bound to the supplied theme service.
    /// </summary>
    /// <param name="themeService">The theme service whose <see cref="ThemeService.CurrentTheme" /> is driven by this VM.</param>
    /// <param name="localization">The localization service used to resolve option display names.</param>
    public ThemeViewModel(ThemeService themeService, LocalizationService localization)
    {
        _themeService = themeService;
        _localization = localization;
        var systemOption = new ThemeOptionViewModel(SystemResourceKey, localization[SystemResourceKey], AppTheme.System);
        var lightOption = new ThemeOptionViewModel(LightResourceKey, localization[LightResourceKey], AppTheme.Light);
        var darkOption = new ThemeOptionViewModel(DarkResourceKey, localization[DarkResourceKey], AppTheme.Dark);
        Options = [systemOption, lightOption, darkOption];
        _selectedOption = FindOption(themeService.CurrentTheme);
        _themeService.ThemeChanged += OnThemeChanged;
        _localization.PropertyChanged += OnLocalizationChanged;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _themeService.ThemeChanged -= OnThemeChanged;
        _localization.PropertyChanged -= OnLocalizationChanged;
    }

    [RelayCommand]
    private void Apply()
    {
        var option = SelectedOption;
        if (option is null)
        {
            return;
        }

        _themeService.SwitchTheme(option.Theme);
    }

    private ThemeOptionViewModel? FindOption(AppTheme theme)
    {
        foreach (var option in Options)
        {
            if (option.Theme == theme)
            {
                return option;
            }
        }

        return null;
    }

    private void OnLocalizationChanged(object? sender, PropertyChangedEventArgs args)
    {
        if (args.PropertyName != nameof(LocalizationService.CurrentCulture))
        {
            return;
        }

        foreach (var option in Options)
        {
            option.DisplayName = _localization[option.ResourceKey];
        }
    }

    private void OnThemeChanged(object? sender, AppTheme theme)
    {
        SelectedOption = FindOption(theme);
    }
}
