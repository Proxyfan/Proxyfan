namespace Proxyfan.Framework.Extensibility;

/// <summary>
///     A single entry in a remote plugin update manifest. Identifies one plugin by its
///     stable id and advertises the latest available version, download URL, and minimum
///     host API version required.
/// </summary>
public sealed class PluginUpdateEntry
{
    /// <summary>
    ///     Gets the download URL for the latest release artifact (typically a zip file).
    /// </summary>
    public required string DownloadUrl { get; init; }

    /// <summary>
    ///     Gets the plugin's stable identifier (matches <c>plugin.yaml</c> <c>id</c>).
    /// </summary>
    public required string Identifier { get; init; }

    /// <summary>
    ///     Gets the latest available version string (SemVer).
    /// </summary>
    public required string LatestVersion { get; init; }

    /// <summary>
    ///     Gets the minimum host API version required by the latest release.
    /// </summary>
    public required string MinimumApiVersion { get; init; }
}
