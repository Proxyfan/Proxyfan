using CommunityToolkit.Mvvm.ComponentModel;
using Proxyfan.Framework.Extensibility;

namespace Proxyfan.Client.Tools.ViewModels;

/// <summary>
///     View model for a single loaded plugin row in the Plugin Manager, exposing the plugin's
///     metadata and load status as bindable strings.
/// </summary>
public sealed partial class PluginItemViewModel : ObservableObject
{
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
    ///     Gets the human-readable error message when load failed, or null on success.
    /// </summary>
    public string? ErrorMessage { get; }

    /// <summary>
    ///     Gets the plugin identifier.
    /// </summary>
    public string Identifier { get; }

    /// <summary>
    ///     Gets a value indicating whether the plugin loaded successfully.
    /// </summary>
    public bool IsLoaded { get; }

    /// <summary>
    ///     Gets the plugin display name.
    /// </summary>
    public string Name { get; }

    /// <summary>
    ///     Gets a human-friendly status label ("Loaded" or "Failed").
    /// </summary>
    public string Status { get; }

    /// <summary>
    ///     Gets the plugin version.
    /// </summary>
    public string Version { get; }

    /// <summary>
    ///     Initializes a new <see cref="PluginItemViewModel" /> wrapping the supplied loaded plugin.
    /// </summary>
    /// <param name="plugin">The loaded plugin to expose.</param>
    public PluginItemViewModel(LoadedPlugin plugin)
    {
        Identifier = plugin.Metadata.Id;
        Name = plugin.Metadata.Name;
        Version = plugin.Metadata.Version;
        Author = plugin.Metadata.Author;
        Description = plugin.Metadata.Description;
        ApiVersion = plugin.Metadata.ApiVersion;
        IsLoaded = plugin.IsLoaded;
        ErrorMessage = plugin.ErrorMessage;
        if (plugin.IsLoaded)
        {
            Status = "Loaded";
        }
        else
        {
            Status = "Failed";
        }
    }
}
