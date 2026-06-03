using System;
using System.Globalization;

namespace Proxyfan.Framework.Extensibility;

/// <summary>
///     Compares two SemVer-style version strings using SemVer 2.0.0 precedence rules.
///     Build metadata (after <c>+</c>) is ignored, and prereleases (after <c>-</c>) are
///     ordered lower than the corresponding stable release. Falls back to lexicographic
///     comparison only when both sides fail to parse as SemVer. Used by
///     <see cref="PluginUpdateAvailabilityResolver" /> to decide whether a manifest entry
///     advertises a newer release than the installed plugin.
/// </summary>
public static class PluginVersionComparer
{
    /// <summary>
    ///     Returns a value indicating whether <paramref name="candidate" /> is strictly
    ///     newer than <paramref name="current" /> per SemVer 2.0.0 precedence rules.
    /// </summary>
    /// <param name="current">The installed version string.</param>
    /// <param name="candidate">The advertised version string.</param>
    /// <returns><see langword="true" /> when <paramref name="candidate" /> is newer.</returns>
    public static bool HasNewerCandidate(string current, string candidate)
    {
        var currentParsed = SemanticVersionParser.Parse(current);
        var candidateParsed = SemanticVersionParser.Parse(candidate);
        if (currentParsed is not null && candidateParsed is not null)
        {
            return Compare(candidateParsed, currentParsed) > 0;
        }

        if (currentParsed is null && candidateParsed is null)
        {
            return string.CompareOrdinal(candidate, current) > 0;
        }

        return false;
    }

    private static int Compare(SemanticVersion left, SemanticVersion right)
    {
        if (left.Major != right.Major)
        {
            return left.Major.CompareTo(right.Major);
        }

        if (left.Minor != right.Minor)
        {
            return left.Minor.CompareTo(right.Minor);
        }

        if (left.Patch != right.Patch)
        {
            return left.Patch.CompareTo(right.Patch);
        }

        return ComparePrerelease(left.PrereleaseIdentifiers, right.PrereleaseIdentifiers);
    }

    private static int CompareIdentifier(string left, string right)
    {
        var leftIsNumeric = HasOnlyDigits(left);
        var rightIsNumeric = HasOnlyDigits(right);
        if (leftIsNumeric && rightIsNumeric)
        {
            return CompareNumericIdentifier(left, right);
        }

        if (leftIsNumeric)
        {
            return -1;
        }

        if (rightIsNumeric)
        {
            return 1;
        }

        return string.CompareOrdinal(left, right);
    }

    private static int CompareNumericIdentifier(string left, string right)
    {
        var leftTrimmed = TrimLeadingZeros(left);
        var rightTrimmed = TrimLeadingZeros(right);
        if (leftTrimmed.Length != rightTrimmed.Length)
        {
            return leftTrimmed.Length.CompareTo(rightTrimmed.Length);
        }

        return string.CompareOrdinal(leftTrimmed, rightTrimmed);
    }

    private static int ComparePrerelease(string[] left, string[] right)
    {
        if (left.Length == 0 && right.Length == 0)
        {
            return 0;
        }

        if (left.Length == 0)
        {
            return 1;
        }

        if (right.Length == 0)
        {
            return -1;
        }

        var shared = Math.Min(left.Length, right.Length);
        for (var index = 0; index < shared; index++)
        {
            var result = CompareIdentifier(left[index], right[index]);
            if (result != 0)
            {
                return result;
            }
        }

        return left.Length.CompareTo(right.Length);
    }

    private static bool HasOnlyDigits(string identifier)
    {
        if (identifier.Length == 0)
        {
            return false;
        }

        foreach (var character in identifier)
        {
            if (character is < '0' or > '9')
            {
                return false;
            }
        }

        return true;
    }

    private static string TrimLeadingZeros(string value)
    {
        var index = 0;
        while (index < value.Length - 1 && value[index] == '0')
        {
            index++;
        }

        return value[index..];
    }

    private static class SemanticVersionParser
    {
        public static SemanticVersion? Parse(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            var withoutBuild = value;
            var plusIndex = withoutBuild.IndexOf('+');
            if (plusIndex >= 0)
            {
                withoutBuild = withoutBuild[..plusIndex];
            }

            string corePart;
            string? prereleasePart;
            var dashIndex = withoutBuild.IndexOf('-');
            if (dashIndex >= 0)
            {
                corePart = withoutBuild[..dashIndex];
                prereleasePart = withoutBuild[(dashIndex + 1)..];
                if (prereleasePart.Length == 0)
                {
                    return null;
                }
            }
            else
            {
                corePart = withoutBuild;
                prereleasePart = null;
            }

            var coreNumbers = ParseCore(corePart);
            if (coreNumbers is null)
            {
                return null;
            }

            var identifiers = Array.Empty<string>();
            if (prereleasePart is not null)
            {
                var parsed = ParsePrerelease(prereleasePart);
                if (parsed is null)
                {
                    return null;
                }

                identifiers = parsed;
            }

            return new SemanticVersion(coreNumbers[0], coreNumbers[1], coreNumbers[2], identifiers);
        }

        private static bool HasLeadingZero(string numeric)
        {
            return numeric.Length > 1 && numeric[0] == '0';
        }

        private static int[]? ParseCore(string core)
        {
            if (core.Length == 0)
            {
                return null;
            }

            var parts = core.Split('.');
            if (parts.Length > 3)
            {
                return null;
            }

            var result = new int[3];
            for (var index = 0; index < parts.Length; index++)
            {
                var parsed = ParseCoreNumber(parts[index]);
                if (parsed is null)
                {
                    return null;
                }

                result[index] = parsed.Value;
            }

            return result;
        }

        private static int? ParseCoreNumber(string part)
        {
            if (HasLeadingZero(part))
            {
                return null;
            }

            if (int.TryParse(part, NumberStyles.None, CultureInfo.InvariantCulture, out var value))
            {
                return value;
            }

            return null;
        }

        private static string[]? ParsePrerelease(string prerelease)
        {
            var parts = prerelease.Split('.');
            for (var index = 0; index < parts.Length; index++)
            {
                var part = parts[index];
                if (part.Length == 0)
                {
                    return null;
                }

                var isNumeric = true;
                foreach (var character in part)
                {
                    var isAllowed = character is (>= '0' and <= '9') or (>= 'A' and <= 'Z') or (>= 'a' and <= 'z') or '-';
                    if (!isAllowed)
                    {
                        return null;
                    }

                    if (character is < '0' or > '9')
                    {
                        isNumeric = false;
                    }
                }

                if (isNumeric && HasLeadingZero(part))
                {
                    return null;
                }
            }

            return parts;
        }
    }

    private sealed class SemanticVersion
    {
        public int Major { get; }

        public int Minor { get; }

        public int Patch { get; }

        public string[] PrereleaseIdentifiers { get; }

        public SemanticVersion(int major, int minor, int patch, string[] prereleaseIdentifiers)
        {
            Major = major;
            Minor = minor;
            Patch = patch;
            PrereleaseIdentifiers = prereleaseIdentifiers;
        }
    }

}
