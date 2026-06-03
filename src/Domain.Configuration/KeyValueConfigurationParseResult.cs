using System.Collections.Generic;

namespace Proxyfan.Domain.Configuration;

/// <summary>
///     Result of parsing key/value configuration text.
/// </summary>
public sealed class KeyValueConfigurationParseResult
{
    /// <summary>
    ///     Gets diagnostics for malformed non-empty, non-comment lines.
    /// </summary>
    public required IReadOnlyList<KeyValueConfigurationParseDiagnostic> Diagnostics { get; init; }

    /// <summary>
    ///     Gets the parsed snapshot.
    /// </summary>
    public required ConfigurationSnapshot Snapshot { get; init; }
}
