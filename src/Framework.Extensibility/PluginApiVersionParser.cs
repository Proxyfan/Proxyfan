using System;
using System.Globalization;

namespace Proxyfan.Framework.Extensibility;

/// <summary>
///     Pure parser for SemVer-style "major.minor" strings used by
///     <see cref="PluginApiVersionChecker" />.
/// </summary>
public static class PluginApiVersionParser
{
    /// <summary>
    ///     Parses "major.minor[.patch]" into a <see cref="PluginApiVersion" />, or returns null
    ///     when either component is missing or non-numeric.
    /// </summary>
    /// <param name="version">The version string.</param>
    /// <returns>The parsed version, or null.</returns>
    public static PluginApiVersion? Parse(string version)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(version);
        var parts = version.Split('.');

        if (parts.Length < 2)
        {
            return null;
        }

        if (!int.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out var major))
        {
            return null;
        }

        if (!int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var minor))
        {
            return null;
        }

        var parsed = new PluginApiVersion(major, minor);
        return parsed;
    }
}
