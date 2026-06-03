using System.Collections.Generic;

namespace Proxyfan.Domain.Configuration;

/// <summary>
///     The outcome of parsing a key-value configuration file.
/// </summary>
public sealed class KeyValueConfigurationParseResult
{
    /// <summary>
    ///     Gets the malformed-line diagnostics emitted while parsing.
    /// </summary>
    public IReadOnlyList<KeyValueConfigurationParseDiagnostic> Diagnostics { get; }

    /// <summary>
    ///     Gets a value indicating whether parsing succeeded without malformed lines.
    /// </summary>
    public bool IsSuccess => Diagnostics.Count == 0;

    /// <summary>
    ///     Gets the parsed snapshot.
    /// </summary>
    public ConfigurationSnapshot Snapshot { get; }

    /// <summary>
    ///     Initializes a new <see cref="KeyValueConfigurationParseResult" />.
    /// </summary>
    /// <param name="snapshot">The parsed snapshot.</param>
    /// <param name="diagnostics">The malformed-line diagnostics.</param>
    public KeyValueConfigurationParseResult(
        ConfigurationSnapshot snapshot,
        IReadOnlyList<KeyValueConfigurationParseDiagnostic> diagnostics)
    {
        Diagnostics = diagnostics;
        Snapshot = snapshot;
    }
}
