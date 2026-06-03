using System.Collections.Generic;

namespace Proxyfan.Domain.Configuration;

/// <summary>
///     Result of parsing key-value configuration text.
/// </summary>
public sealed class KeyValueConfigurationParseResult
{
    /// <summary>
    ///     Gets a value indicating whether the parse completed without malformed lines.
    /// </summary>
    public bool IsSuccessful => ParseDiagnostics.Count == 0;

    /// <summary>
    ///     Gets diagnostics for malformed lines that were encountered during parsing.
    /// </summary>
    public required IReadOnlyList<string> ParseDiagnostics { get; init; }

    /// <summary>
    ///     Gets the parsed snapshot.
    /// </summary>
    public required ConfigurationSnapshot Snapshot { get; init; }
}
