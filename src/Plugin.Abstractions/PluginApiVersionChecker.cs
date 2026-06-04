using System;

namespace Proxyfan.Plugin.Abstractions;

/// <summary>
///     Compares plugin-declared API version requirements against the host's actual API version.
/// </summary>
public static class PluginApiVersionChecker
{
    /// <summary>
    ///     Returns <see langword="true" /> when the plugin's declared API version is compatible
    ///     with the host's API version. Compatibility rules:
    ///     <list type="bullet">
    ///       <item>Major versions MUST match.</item>
    ///       <item>The host's minor version MUST be greater than or equal to the plugin's minor version.</item>
    ///     </list>
    /// </summary>
    /// <param name="hostApiVersion">The host's API version (e.g. <c>"1.5.0"</c>).</param>
    /// <param name="pluginRequiredVersion">The plugin's required API version.</param>
    /// <returns><see langword="true" /> when compatible.</returns>
    public static bool HasCompatibility(string hostApiVersion, string pluginRequiredVersion)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(hostApiVersion);
        ArgumentException.ThrowIfNullOrWhiteSpace(pluginRequiredVersion);

        var host = PluginApiVersionParser.Parse(hostApiVersion);
        var plugin = PluginApiVersionParser.Parse(pluginRequiredVersion);

        if (host is null || plugin is null)
        {
            return false;
        }

        if (host.Major != plugin.Major)
        {
            return false;
        }

        return host.Minor >= plugin.Minor;
    }
}
