namespace Proxyfan.Client.Tools.ViewModels;

/// <summary>
///     Immutable Client-owned projection of an available plugin update.
/// </summary>
public sealed class PluginUpdateAvailabilityModel
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
    ///     Initializes a new <see cref="PluginUpdateAvailabilityModel" />.
    /// </summary>
    /// <param name="identifier">The plugin identifier.</param>
    /// <param name="name">The plugin display name.</param>
    /// <param name="author">The plugin author.</param>
    /// <param name="currentVersion">The installed version.</param>
    /// <param name="latestVersion">The latest available version.</param>
    /// <param name="downloadUrl">The manifest-advertised download URL.</param>
    /// <param name="isCompatible">Whether the update is API-compatible with the host.</param>
    public PluginUpdateAvailabilityModel(
        string identifier,
        string name,
        string author,
        string currentVersion,
        string latestVersion,
        string downloadUrl,
        bool isCompatible)
    {
        Identifier = identifier;
        Name = name;
        Author = author;
        CurrentVersion = currentVersion;
        LatestVersion = latestVersion;
        DownloadUrl = downloadUrl;
        IsCompatible = isCompatible;
    }
}
