using System.Collections.Generic;

namespace Proxyfan.Domain.Configuration;

/// <summary>
///     Outcome of <see cref="KeyValueConfigurationParser.Parse" />. Carries the
///     successfully-parsed snapshot together with any lines that could not be
///     interpreted as <c>key=value</c> pairs so that callers can surface diagnostics
///     rather than silently discarding malformed input.
/// </summary>
public sealed class KeyValueConfigurationParseResult
{
    /// <summary>
    ///     Gets a value indicating whether parsing found no malformed lines. Equivalent
    ///     to <c><see cref="MalformedLines" />.Count == 0</c>.
    /// </summary>
    public bool IsSuccess { get; }

    /// <summary>
    ///     Gets the raw trimmed text of each line that could not be parsed as a
    ///     <c>key=value</c> pair. Empty when <see cref="IsSuccess" /> is
    ///     <see langword="true" />.
    /// </summary>
    public IReadOnlyList<string> MalformedLines { get; }

    /// <summary>
    ///     Gets the snapshot of the valid key-value pairs extracted from the input.
    ///     Populated even when <see cref="IsSuccess" /> is <see langword="false" />
    ///     so callers can inspect the partial result for diagnostic purposes.
    /// </summary>
    public ConfigurationSnapshot Snapshot { get; }

    /// <summary>
    ///     Initializes a new <see cref="KeyValueConfigurationParseResult" />. Use
    ///     <see cref="KeyValueConfigurationParseResults.Success" /> or
    ///     <see cref="KeyValueConfigurationParseResults.Failure" /> for typical
    ///     construction.
    /// </summary>
    /// <param name="snapshot">The partial-or-complete parsed snapshot.</param>
    /// <param name="malformedLines">The raw trimmed text of any malformed lines.</param>
    /// <param name="isSuccess">Whether parsing found no malformed lines.</param>
    public KeyValueConfigurationParseResult(
        ConfigurationSnapshot snapshot,
        IReadOnlyList<string> malformedLines,
        bool isSuccess)
    {
        Snapshot = snapshot;
        MalformedLines = malformedLines;
        IsSuccess = isSuccess;
    }
}
