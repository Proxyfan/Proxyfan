namespace Proxyfan.Framework.Extensibility;

/// <summary>
///     Holds the absolute URL the plugin manager uses when checking for plugin updates.
///     An empty URL signals the feature is disabled and the manager should not invoke
///     <see cref="IPluginUpdateFeed.FetchAsync" />.
/// </summary>
public sealed class PluginUpdateManifestUrlProvider
{
    private readonly string _manifestUrl;

    /// <summary>
    ///     Initializes a new <see cref="PluginUpdateManifestUrlProvider" />.
    /// </summary>
    /// <param name="manifestUrl">The manifest URL; pass empty to disable update checking.</param>
    public PluginUpdateManifestUrlProvider(string manifestUrl)
    {
        _manifestUrl = manifestUrl ?? string.Empty;
    }

    /// <summary>
    ///     Returns the configured manifest URL. Returns an empty string when not configured.
    /// </summary>
    /// <returns>The configured manifest URL.</returns>
    public string GetManifestUrl()
    {
        return _manifestUrl;
    }

    /// <summary>
    ///     Returns a value indicating whether a manifest URL has been configured.
    /// </summary>
    /// <returns><see langword="true" /> when configured.</returns>
    public bool HasConfiguration()
    {
        return !string.IsNullOrWhiteSpace(_manifestUrl);
    }
}
