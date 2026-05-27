namespace Proxyfan.Domain.Updates;

/// <summary>
///     Compares semantic versions (X.Y.Z) ignoring any pre-release or build metadata. Used
///     by <see cref="IUpdateChecker" /> implementations to decide whether a remote release is
///     newer than the locally installed version.
/// </summary>
public static class SemanticVersionComparer
{
    /// <summary>
    ///     Returns true when <paramref name="candidate" /> is a strictly newer semantic
    ///     version than <paramref name="current" />.
    /// </summary>
    /// <param name="current">The currently installed version.</param>
    /// <param name="candidate">The candidate version to compare against.</param>
    /// <returns>True when candidate &gt; current.</returns>
    /// <exception cref="System.ArgumentException">
    ///     Thrown when either version cannot be parsed as a valid semantic version.
    /// </exception>
    public static bool HasNewerVersion(string current, string candidate)
    {
        var currentParts = ParseParts(current);
        var candidateParts = ParseParts(candidate);

        for (var index = 0; index < 3; index++)
        {
            if (candidateParts[index] > currentParts[index])
            {
                return true;
            }

            if (candidateParts[index] < currentParts[index])
            {
                return false;
            }
        }

        return false;
    }

    private static int[] ParseParts(string version)
    {
        var trimmed = TrimMetadata(version);
        var rawParts = trimmed.Split('.');

        if (rawParts.Length < 3)
        {
            throw new System.ArgumentException($"Version '{version}' must have at least three dot-separated components.", nameof(version));
        }

        var parts = new int[3];

        for (var index = 0; index < 3; index++)
        {
            if (!int.TryParse(rawParts[index], System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out var value))
            {
                throw new System.ArgumentException($"Version component '{rawParts[index]}' is not a valid integer.", nameof(version));
            }

            parts[index] = value;
        }

        return parts;
    }

    private static string TrimMetadata(string version)
    {
        var preReleaseIndex = version.IndexOf('-', System.StringComparison.Ordinal);

        if (preReleaseIndex >= 0)
        {
            return version[..preReleaseIndex];
        }

        var buildIndex = version.IndexOf('+', System.StringComparison.Ordinal);

        if (buildIndex >= 0)
        {
            return version[..buildIndex];
        }

        return version;
    }
}
