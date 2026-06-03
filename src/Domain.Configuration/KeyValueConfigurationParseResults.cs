using System.Collections.Generic;

namespace Proxyfan.Domain.Configuration;

/// <summary>
///     Factory helpers for constructing <see cref="KeyValueConfigurationParseResult" />
///     instances.
/// </summary>
public static class KeyValueConfigurationParseResults
{
    /// <summary>
    ///     Constructs a failure result carrying the partial snapshot and the malformed lines.
    /// </summary>
    /// <param name="snapshot">The partial snapshot of valid lines.</param>
    /// <param name="malformedLines">The raw trimmed text of the malformed lines.</param>
    /// <returns>The failure result.</returns>
    public static KeyValueConfigurationParseResult Failure(
        ConfigurationSnapshot snapshot,
        IReadOnlyList<string> malformedLines)
    {
        var result = new KeyValueConfigurationParseResult(snapshot, malformedLines, false);
        return result;
    }

    /// <summary>
    ///     Constructs a success result carrying the fully-parsed snapshot.
    /// </summary>
    /// <param name="snapshot">The parsed snapshot.</param>
    /// <returns>The success result.</returns>
    public static KeyValueConfigurationParseResult Success(ConfigurationSnapshot snapshot)
    {
        var result = new KeyValueConfigurationParseResult(snapshot, [], true);
        return result;
    }
}
