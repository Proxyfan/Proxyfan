using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Proxyfan.Framework.Extensibility;
using System;
using System.IO;

namespace Proxyfan.Client.Tools.ViewModels;

/// <summary>
///     View model for a single plugin row in the Plugin Manager. Exposes the plugin's
///     metadata and load status as bindable strings, surfaces the user-controlled
///     enabled state with persistence through <see cref="IPluginEnabledStateStore" />,
///     and offers Open Folder + Remove commands for managing the plugin on disk.
/// </summary>
public sealed partial class PluginItemViewModel : ObservableObject
{
    private readonly IPluginEnabledStateStore _enabledStateStore;
    private readonly IPluginFolderOpener _folderOpener;
    private readonly PluginStateChangedCallback _onStateChanged;
    [ObservableProperty]
    private string? _errorMessage;
    [ObservableProperty]
    private bool _isEnabled;

    /// <summary>
    ///     Gets the API version reported by the plugin.
    /// </summary>
    public string ApiVersion { get; }

    /// <summary>
    ///     Gets the plugin author.
    /// </summary>
    public string Author { get; }

    /// <summary>
    ///     Gets the plugin description.
    /// </summary>
    public string Description { get; }

    /// <summary>
    ///     Gets the plugin identifier.
    /// </summary>
    public string Identifier { get; }

    /// <summary>
    ///     Gets a value indicating whether <see cref="OpenFolderCommand" /> can be invoked —
    ///     true when the plugin has an associated source directory that still exists on
    ///     disk, otherwise false.
    /// </summary>
    public bool IsFolderAvailable => SourceDirectory is not null && Directory.Exists(SourceDirectory);

    /// <summary>
    ///     Gets a value indicating whether the plugin loaded successfully.
    /// </summary>
    public bool IsLoaded { get; }

    /// <summary>
    ///     Gets the plugin display name.
    /// </summary>
    public string Name { get; }

    /// <summary>
    ///     Gets the absolute path of the plugin's source directory on disk, or null when
    ///     the entry did not originate from a discovered directory.
    /// </summary>
    public string? SourceDirectory { get; }

    /// <summary>
    ///     Gets a human-friendly status label ("Loaded", "Failed", or "Disabled").
    /// </summary>
    public string Status { get; }

    /// <summary>
    ///     Gets the plugin version.
    /// </summary>
    public string Version { get; }

    /// <summary>
    ///     Initializes a new <see cref="PluginItemViewModel" /> wrapping the supplied loaded
    ///     plugin.
    /// </summary>
    /// <param name="plugin">The loaded plugin to expose.</param>
    /// <param name="enabledStateStore">The store used to read + persist the user's enable choice.</param>
    /// <param name="folderOpener">The folder opener invoked by the Open Folder command.</param>
    /// <param name="onStateChanged">Callback fired whenever the user toggles the enabled state or removes the plugin; the parent view model uses this to mark a restart as required and to refresh the snapshot.</param>
    public PluginItemViewModel(
        LoadedPlugin plugin,
        IPluginEnabledStateStore enabledStateStore,
        IPluginFolderOpener folderOpener,
        PluginStateChangedCallback onStateChanged)
    {
        _enabledStateStore = enabledStateStore;
        _folderOpener = folderOpener;
        _onStateChanged = onStateChanged;
        Identifier = plugin.Metadata.Id;
        Name = plugin.Metadata.Name;
        Version = plugin.Metadata.Version;
        Author = plugin.Metadata.Author;
        Description = plugin.Metadata.Description;
        ApiVersion = plugin.Metadata.ApiVersion;
        IsLoaded = plugin.IsLoaded;
        ErrorMessage = plugin.ErrorMessage;
        SourceDirectory = plugin.SourceDirectory;
        if (plugin.IsLoaded)
        {
            Status = "Loaded";
        }
        else
        {
            Status = "Failed";
        }

        var disabled = enabledStateStore.GetDisabledIdentifiers();
        _isEnabled = !disabled.Contains(Identifier);
    }

    partial void OnIsEnabledChanged(bool value)
    {
        _enabledStateStore.SetEnabled(Identifier, value);
        _onStateChanged();
    }

    [RelayCommand]
    private void OpenFolder()
    {
        if (SourceDirectory is null)
        {
            return;
        }

        _folderOpener.Open(SourceDirectory);
    }

    [RelayCommand]
    private void Remove()
    {
        if (SourceDirectory is not null)
        {
            try
            {
                Directory.Delete(SourceDirectory, recursive: true);
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Failed to remove plugin folder: {ex.Message}";
                return;
            }
        }

        _enabledStateStore.SetEnabled(Identifier, false);
        _onStateChanged();
    }
}
