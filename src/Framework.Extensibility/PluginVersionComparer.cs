using System.Globalization;

namespace Proxyfan.Framework.Extensibility;

/// <summary>
///     Compares two SemVer-style version strings. Falls back to lexicographic comparison
///     when either side is not parseable. Used by <see cref="PluginUpdateAvailabilityResolver" />
///     to decide whether a manifest entry advertises a newer release than the installed
///     plugin.
/// </summary>
public static class PluginVersionComparer
{
    /// <summary>
    ///     Returns a value indicating whether <paramref name="candidate" /> is strictly
    ///     newer than <paramref name="current" />.
    /// </summary>
    /// <param name="current">The installed version string.</param>
    /// <param name="candidate">The advertised version string.</param>
    /// <returns><see langword="true" /> when <paramref name="candidate" /> is newer.</returns>
    public static bool HasNewerCandidate(string current, string candidate)
    {
        var currentParsed = ParseTriple(current);
        var candidateParsed = ParseTriple(candidate);
        if (currentParsed is null || candidateParsed is null)
        {
            return string.CompareOrdinal(candidate, current) > 0;
        }

        if (candidateParsed.Major != currentParsed.Major)
        {
            return candidateParsed.Major > currentParsed.Major;
        }

        if (candidateParsed.Minor != currentParsed.Minor)
        {
            return candidateParsed.Minor > currentParsed.Minor;
        }

        return candidateParsed.Patch > currentParsed.Patch;
    }

    private static VersionTriple? ParseTriple(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var parts = value.Split('.');
        if (parts.Length < 1)
        {
            return null;
        }

        if (!int.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out var major))
        {
            return null;
        }

        var minor = 0;
        if (parts.Length >= 2 && !int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out minor))
        {
            return null;
        }

        var patch = 0;
        if (parts.Length >= 3 && !int.TryParse(parts[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out patch))
        {
            return null;
        }

        var triple = new VersionTriple(major, minor, patch);
        return triple;
    }

    private sealed class VersionTriple
    {
        public int Major { get; }

        public int Minor { get; }

        public int Patch { get; }

        public VersionTriple(int major, int minor, int patch)
        {
            Major = major;
            Minor = minor;
            Patch = patch;
        }
    }
}
