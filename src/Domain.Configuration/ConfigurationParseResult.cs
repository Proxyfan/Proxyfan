using System.Collections.Generic;

namespace Proxyfan.Domain.Configuration;

/// <summary>
///     Result produced when parsing key-value configuration text.
/// </summary>
public sealed class ConfigurationParseResult
{
    /// <summary>
    ///     Gets a value indicating whether any malformed lines were encountered.
    /// </summary>
    public bool IsMalformedLinesPresent => MalformedLines.Count > 0;

    /// <summary>
    ///     Gets the malformed non-empty, non-comment lines that were encountered while parsing.
    /// </summary>
    public required IReadOnlyList<string> MalformedLines { get; init; }

    /// <summary>
    ///     Gets the parsed snapshot.
    /// </summary>
    public required ConfigurationSnapshot Snapshot { get; init; }
}
