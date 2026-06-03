using System.Collections.Generic;

namespace Proxyfan.Domain.Configuration;

/// <summary>
///     The result of <see cref="KeyValueConfigurationParser.Parse" />. Bundles the parsed
///     <see cref="ConfigurationSnapshot" /> with any parse diagnostics so callers can
///     distinguish intentionally absent settings from malformed lines that were discarded.
/// </summary>
public sealed class ConfigurationParseResult
{
    /// <summary>
    ///     Gets a value indicating whether any malformed lines were encountered during
    ///     parsing.
    /// </summary>
    public bool HasMalformedLines => MalformedLines.Count > 0;

    /// <summary>
    ///     Gets the list of diagnostics, one per malformed line, in source order.
    /// </summary>
    public required IReadOnlyList<ConfigurationParseDiagnostic> MalformedLines { get; init; }

    /// <summary>
    ///     Gets the configuration snapshot containing all successfully parsed key-value
    ///     pairs. Lines reported in <see cref="MalformedLines" /> are not present here.
    /// </summary>
    public required ConfigurationSnapshot Snapshot { get; init; }
}
