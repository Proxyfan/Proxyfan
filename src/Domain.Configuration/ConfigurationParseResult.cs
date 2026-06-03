using System.Collections.Generic;

namespace Proxyfan.Domain.Configuration;

/// <summary>
///     The result of <see cref="KeyValueConfigurationParser.Parse" />. Contains the
///     parsed <see cref="Snapshot" /> together with the 1-based line numbers of any lines
///     that were not well-formed (neither empty, nor a comment, nor a valid
///     <c>key=value</c> pair).
/// </summary>
public sealed class ConfigurationParseResult
{
    /// <summary>
    ///     Gets the 1-based line numbers of the lines that were malformed (neither empty,
    ///     nor a comment, nor a valid <c>key=value</c> pair) and were therefore skipped
    ///     during parsing.
    /// </summary>
    public required IReadOnlyList<int> MalformedLineNumbers { get; init; }

    /// <summary>
    ///     Gets the snapshot of key-value pairs successfully parsed from well-formed lines.
    /// </summary>
    public required ConfigurationSnapshot Snapshot { get; init; }
}
