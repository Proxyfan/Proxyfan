using CommunityToolkit.Mvvm.ComponentModel;

namespace Proxyfan.Client.Tools.ViewModels;

/// <summary>
///     A single available plugin update row displayed in the Plugin Manager updates panel.
///     Wraps a <see cref="PluginUpdateAvailabilitySnapshot" /> with bindable string properties and
///     a derived label suitable for direct binding.
/// </summary>
public sealed partial class PluginUpdateAvailabilityViewModel : ObservableObject
{
    /// <summary>
    ///     Gets the plugin author.
    /// </summary>
    public string Author { get; }

    /// <summary>
    ///     Gets the installed version.
    /// </summary>
    public string CurrentVersion { get; }

    /// <summary>
    ///     Gets the manifest-advertised download URL.
    /// </summary>
    public string DownloadUrl { get; }

    /// <summary>
    ///     Gets the plugin identifier.
    /// </summary>
    public string Identifier { get; }

    /// <summary>
    ///     Gets a value indicating whether the manifest entry's minimum API version is
    ///     compatible with the host's API version.
    /// </summary>
    public bool IsCompatible { get; }

    /// <summary>
    ///     Gets the latest version.
    /// </summary>
    public string LatestVersion { get; }

    /// <summary>
    ///     Gets the plugin display name.
    /// </summary>
    public string Name { get; }

    /// <summary>
    ///     Gets the version transition shown to the user — "current → latest" or
    ///     "current → latest (incompatible)" when the host API is too old.
    /// </summary>
    public string VersionTransition { get; }

    /// <summary>
    ///     Initializes a new <see cref="PluginUpdateAvailabilityViewModel" />.
    /// </summary>
    /// <param name="availability">The availability entry to wrap.</param>
    public PluginUpdateAvailabilityViewModel(PluginUpdateAvailabilitySnapshot availability)
    {
        Identifier = availability.Identifier;
        Name = availability.Name;
        Author = availability.Author;
        CurrentVersion = availability.CurrentVersion;
        LatestVersion = availability.LatestVersion;
        DownloadUrl = availability.DownloadUrl;
        IsCompatible = availability.IsCompatible;
        if (availability.IsCompatible)
        {
            VersionTransition = $"{availability.CurrentVersion} → {availability.LatestVersion}";
        }
        else
        {
            VersionTransition = $"{availability.CurrentVersion} → {availability.LatestVersion} (incompatible)";
        }
    }
}
