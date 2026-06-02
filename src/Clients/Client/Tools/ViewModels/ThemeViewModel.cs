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
///     Theme option display names are sourced from <see cref="LocalizationService" /> and
///     refreshed on runtime locale changes.
/// </summary>
public sealed partial class ThemeViewModel : ObservableObject, IDisposable
{
    private const string DarkOptionKey = "Tools_Theme_Option_Dark";
    private const string LightOptionKey = "Tools_Theme_Option_Light";
    private const string SystemOptionKey = "Tools_Theme_Option_System";
    private readonly LocalizationService _localizationService;
    private readonly ThemeService _themeService;
    [ObservableProperty]
    private ThemeOptionViewModel? _selectedOption;

    /// <summary>
    ///     Gets the list of available theme options.
    /// </summary>
    public ObservableCollection<ThemeOptionViewModel> Options { get; }

    /// <summary>
    ///     Initializes a new <see cref="ThemeViewModel" /> bound to the supplied theme and
    ///     localization services.
    /// </summary>
    /// <param name="themeService">The theme service whose <see cref="ThemeService.CurrentTheme" /> is driven by this VM.</param>
    /// <param name="localizationService">The localization service used to resolve theme option display names.</param>
    public ThemeViewModel(ThemeService themeService, LocalizationService localizationService)
    {
        _themeService = themeService;
        _localizationService = localizationService;
        var systemOption = new ThemeOptionViewModel(SystemOptionKey, _localizationService[SystemOptionKey], AppTheme.System);
        var lightOption = new ThemeOptionViewModel(LightOptionKey, _localizationService[LightOptionKey], AppTheme.Light);
        var darkOption = new ThemeOptionViewModel(DarkOptionKey, _localizationService[DarkOptionKey], AppTheme.Dark);
        Options = [systemOption, lightOption, darkOption];
        _selectedOption = FindOption(themeService.CurrentTheme);
        _themeService.ThemeChanged += OnThemeChanged;
        _localizationService.PropertyChanged += OnLocalizationChanged;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _themeService.ThemeChanged -= OnThemeChanged;
        _localizationService.PropertyChanged -= OnLocalizationChanged;
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
        foreach (var option in Options)
        {
            option.DisplayName = _localizationService[option.ResourceKey];
        }
    }

    private void OnThemeChanged(object? sender, AppTheme theme)
    {
        SelectedOption = FindOption(theme);
    }
}
