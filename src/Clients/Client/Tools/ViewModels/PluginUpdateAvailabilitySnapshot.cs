namespace Proxyfan.Client.Tools.ViewModels;

/// <summary>
///     Immutable client-owned data snapshot for a single available plugin update.
/// </summary>
public sealed class PluginUpdateAvailabilitySnapshot
{
    /// <summary>
    ///     Gets the plugin author.
    /// </summary>
    public required string Author { get; init; }

    /// <summary>
    ///     Gets the installed version.
    /// </summary>
    public required string CurrentVersion { get; init; }

    /// <summary>
    ///     Gets the manifest-advertised download URL.
    /// </summary>
    public required string DownloadUrl { get; init; }

    /// <summary>
    ///     Gets the plugin identifier.
    /// </summary>
    public required string Identifier { get; init; }

    /// <summary>
    ///     Gets a value indicating whether the advertised update is host-compatible.
    /// </summary>
    public required bool IsCompatible { get; init; }

    /// <summary>
    ///     Gets the latest version.
    /// </summary>
    public required string LatestVersion { get; init; }

    /// <summary>
    ///     Gets the plugin display name.
    /// </summary>
    public required string Name { get; init; }
}
