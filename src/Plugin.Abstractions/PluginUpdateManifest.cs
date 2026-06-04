using System.Collections.Generic;

namespace Proxyfan.Plugin.Abstractions;

/// <summary>
///     A parsed plugin update manifest fetched from a remote source. Contains zero or more
///     <see cref="PluginUpdateEntry" /> rows, one per advertised plugin.
/// </summary>
public sealed class PluginUpdateManifest
{
    /// <summary>
    ///     Gets the list of plugin entries advertised by the manifest.
    /// </summary>
    public required IReadOnlyList<PluginUpdateEntry> Plugins { get; init; }
}
