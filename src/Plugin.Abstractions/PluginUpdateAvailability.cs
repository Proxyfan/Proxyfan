namespace Proxyfan.Plugin.Abstractions;

/// <summary>
///     One available plugin update, computed by joining an <see cref="IPluginLoadState" /> in
///     the registry with the matching <see cref="PluginUpdateEntry" /> in a remote manifest.
///     <para>
///         The resolver only emits an entry when the manifest version is strictly newer
///         than the installed version per <see cref="PluginVersionComparer" />. The
///         <see cref="IsCompatible" /> flag indicates whether the host's API version
///         satisfies the manifest entry's <see cref="PluginUpdateEntry.MinimumApiVersion" />
///         (per <see cref="PluginApiVersionChecker" /> rules).
///     </para>
/// </summary>
public sealed class PluginUpdateAvailability
{
    /// <summary>
    ///     Gets the plugin author (carried through from the installed plugin's metadata).
    /// </summary>
    public required string Author { get; init; }

    /// <summary>
    ///     Gets the currently-installed plugin version.
    /// </summary>
    public required string CurrentVersion { get; init; }

    /// <summary>
    ///     Gets the manifest-advertised download URL.
    /// </summary>
    public required string DownloadUrl { get; init; }

    /// <summary>
    ///     Gets the plugin's stable identifier.
    /// </summary>
    public required string Identifier { get; init; }

    /// <summary>
    ///     Gets a value indicating whether the manifest entry's minimum API version is
    ///     compatible with the host's API version.
    /// </summary>
    public required bool IsCompatible { get; init; }

    /// <summary>
    ///     Gets the manifest-advertised latest version.
    /// </summary>
    public required string LatestVersion { get; init; }

    /// <summary>
    ///     Gets the plugin display name.
    /// </summary>
    public required string Name { get; init; }
}
