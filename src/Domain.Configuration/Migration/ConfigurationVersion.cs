using System;
using System.Globalization;

namespace Proxyfan.Domain.Configuration.Migration;

/// <summary>
///     Represents an immutable, comparable configuration schema version expressed as
///     <c>Major.Minor</c>. Used by the configuration migration pipeline to determine whether
///     a stored configuration needs to be transformed before being interpreted by the running
///     application.
/// </summary>
public readonly record struct ConfigurationVersion : IComparable<ConfigurationVersion>
{
    /// <summary>
    ///     Gets the major version component.
    /// </summary>
    public int Major { get; init; }

    /// <summary>
    ///     Gets the minor version component.
    /// </summary>
    public int Minor { get; init; }

    /// <summary>
    ///     Initializes a new <see cref="ConfigurationVersion" /> with the supplied components.
    /// </summary>
    /// <param name="major">The non-negative major version component.</param>
    /// <param name="minor">The non-negative minor version component.</param>
    public ConfigurationVersion(int major, int minor)
    {
        if (major < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(major), major, "Major version must be non-negative.");
        }

        if (minor < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(minor), minor, "Minor version must be non-negative.");
        }

        Major = major;
        Minor = minor;
    }

    /// <summary>
    ///     Compares this version to <paramref name="other" /> using major-then-minor ordering.
    /// </summary>
    /// <param name="other">The version to compare against.</param>
    /// <returns>
    ///     A negative number when this is less than <paramref name="other" />, zero when equal,
    ///     positive when greater.
    /// </returns>
    public int CompareTo(ConfigurationVersion other)
    {
        if (Major != other.Major)
        {
            return Major.CompareTo(other.Major);
        }

        return Minor.CompareTo(other.Minor);
    }

    /// <summary>
    ///     Returns the <c>Major.Minor</c> textual representation.
    /// </summary>
    /// <returns>The version as <c>Major.Minor</c>.</returns>
    public override string ToString()
    {
        return string.Create(CultureInfo.InvariantCulture, $"{Major}.{Minor}");
    }

    /// <summary>
    ///     Determines whether <paramref name="left" /> is strictly less than <paramref name="right" />.
    /// </summary>
    /// <param name="left">The left operand.</param>
    /// <param name="right">The right operand.</param>
    /// <returns><see langword="true" /> when <paramref name="left" /> is strictly less.</returns>
    public static bool operator <(ConfigurationVersion left, ConfigurationVersion right)
    {
        return left.CompareTo(right) < 0;
    }

    /// <summary>
    ///     Determines whether <paramref name="left" /> is less than or equal to
    ///     <paramref name="right" />.
    /// </summary>
    /// <param name="left">The left operand.</param>
    /// <param name="right">The right operand.</param>
    /// <returns><see langword="true" /> when <paramref name="left" /> is less than or equal.</returns>
    public static bool operator <=(ConfigurationVersion left, ConfigurationVersion right)
    {
        return left.CompareTo(right) <= 0;
    }

    /// <summary>
    ///     Determines whether <paramref name="left" /> is strictly greater than
    ///     <paramref name="right" />.
    /// </summary>
    /// <param name="left">The left operand.</param>
    /// <param name="right">The right operand.</param>
    /// <returns><see langword="true" /> when <paramref name="left" /> is strictly greater.</returns>
    public static bool operator >(ConfigurationVersion left, ConfigurationVersion right)
    {
        return left.CompareTo(right) > 0;
    }

    /// <summary>
    ///     Determines whether <paramref name="left" /> is greater than or equal to
    ///     <paramref name="right" />.
    /// </summary>
    /// <param name="left">The left operand.</param>
    /// <param name="right">The right operand.</param>
    /// <returns>
    ///     <see langword="true" /> when <paramref name="left" /> is greater than or equal.
    /// </returns>
    public static bool operator >=(ConfigurationVersion left, ConfigurationVersion right)
    {
        return left.CompareTo(right) >= 0;
    }

    /// <summary>
    ///     Parses a <c>Major.Minor</c> textual representation into a
    ///     <see cref="ConfigurationVersion" />.
    /// </summary>
    /// <param name="text">The textual representation, e.g. <c>"1.0"</c> or <c>"2.10"</c>.</param>
    /// <returns>The parsed <see cref="ConfigurationVersion" />.</returns>
    /// <exception cref="FormatException">
    ///     The supplied text was not in <c>Major.Minor</c> format with non-negative integer
    ///     components.
    /// </exception>
    public static ConfigurationVersion Parse(string text)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(text);
        var separatorIndex = text.IndexOf('.', StringComparison.Ordinal);
        if (separatorIndex <= 0 || separatorIndex == text.Length - 1)
        {
            throw new FormatException($"Configuration version '{text}' is not in 'Major.Minor' format.");
        }

        var majorText = text[..separatorIndex];
        var minorText = text[(separatorIndex + 1)..];
        if (!int.TryParse(majorText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var major)
            || !int.TryParse(minorText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var minor))
        {
            throw new FormatException($"Configuration version '{text}' contains non-integer components.");
        }

        var version = new ConfigurationVersion(major, minor);
        return version;
    }

    /// <summary>
    ///     Determines whether this version is strictly less than <paramref name="other" />.
    /// </summary>
    /// <param name="other">The version to compare against.</param>
    /// <returns><see langword="true" /> when this version is strictly less.</returns>
    public bool HasLowerOrderThan(ConfigurationVersion other)
    {
        return CompareTo(other) < 0;
    }
}
