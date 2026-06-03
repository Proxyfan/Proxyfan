using System.Collections.Generic;

namespace Proxyfan.Domain.Configuration;

/// <summary>
///     Result produced by <see cref="KeyValueConfigurationParser.Parse" /> containing the
///     parsed snapshot and any malformed line diagnostics.
/// </summary>
public sealed class KeyValueConfigurationParseResult
{
    /// <summary>
    ///     Gets line numbers for non-empty/non-comment lines that are malformed and could
    ///     not be parsed as valid <c>key=value</c> entries.
    /// </summary>
    public required IReadOnlyList<int> MalformedLineNumbers { get; init; }

    /// <summary>
    ///     Gets the parsed snapshot containing all valid key/value entries.
    /// </summary>
    public required ConfigurationSnapshot Snapshot { get; init; }
}
