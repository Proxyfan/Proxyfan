using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Proxyfan.Presentation.Theming;
using System;
using System.Collections.ObjectModel;

namespace Proxyfan.Client.Tools.ViewModels;

/// <summary>
///     View model for the Theme picker tool window. Lets the user choose between
///     System, Light, and Dark themes; the selection is applied through <see cref="ThemeService" />.
/// </summary>
public sealed partial class ThemeViewModel : ObservableObject, IDisposable
{
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
    public ThemeViewModel(ThemeService themeService)
    {
        _themeService = themeService;
        var systemOption = new ThemeOptionViewModel("System", AppTheme.System);
        var lightOption = new ThemeOptionViewModel("Light", AppTheme.Light);
        var darkOption = new ThemeOptionViewModel("Dark", AppTheme.Dark);
        Options = [systemOption, lightOption, darkOption];
        _selectedOption = FindOption(themeService.CurrentTheme);
        _themeService.ThemeChanged += OnThemeChanged;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _themeService.ThemeChanged -= OnThemeChanged;
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

    private void OnThemeChanged(object? sender, AppTheme theme)
    {
        SelectedOption = FindOption(theme);
    }
}
