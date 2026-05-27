using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Proxyfan.Framework.Extensibility;
using System.Collections.ObjectModel;

namespace Proxyfan.Client.Tools.ViewModels;

/// <summary>
///     View model for the Plugin Manager tool window. Exposes the list of loaded plugins
///     surfaced by <see cref="PluginRegistry" /> and provides a refresh command for
///     re-reading the registry snapshot.
/// </summary>
public sealed partial class PluginManagerViewModel : ObservableObject
{
    private readonly PluginRegistry _registry;
    [ObservableProperty]
    private string _summary;

    /// <summary>
    ///     Gets the observable collection of plugin rows currently displayed.
    /// </summary>
    public ObservableCollection<PluginItemViewModel> Plugins { get; }

    /// <summary>
    ///     Initializes a new <see cref="PluginManagerViewModel" /> bound to the supplied
    ///     plugin registry.
    /// </summary>
    /// <param name="registry">The plugin registry to expose.</param>
    public PluginManagerViewModel(PluginRegistry registry)
    {
        _registry = registry;
        _summary = string.Empty;
        Plugins = [];
        RefreshSnapshot();
    }

    [RelayCommand]
    private void Refresh()
    {
        RefreshSnapshot();
    }

    private void RefreshSnapshot()
    {
        Plugins.Clear();
        var totalCount = 0;
        var failedCount = 0;
        foreach (var plugin in _registry.Plugins)
        {
            var viewModel = new PluginItemViewModel(plugin);
            Plugins.Add(viewModel);
            totalCount++;
            if (!plugin.IsLoaded)
            {
                failedCount++;
            }
        }

        var loadedCount = totalCount - failedCount;
        Summary = $"{totalCount} plugin(s) registered — {loadedCount} loaded, {failedCount} failed.";
    }
}
