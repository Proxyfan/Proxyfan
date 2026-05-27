namespace Proxyfan.Framework.Networking;

/// <summary>
///     Per-literal-representation options used by the HPACK decoder when dispatching
///     incremental-indexed, never-indexed, and without-indexing literals.
/// </summary>
public sealed class HypertextTransferProtocolVersion2HpackLiteralOptions
{
    /// <summary>
    ///     Gets a value indicating whether the decoded entry must be appended to the
    ///     dynamic table after emission.
    /// </summary>
    public required bool IsAppendingToDynamicTable { get; init; }

    /// <summary>
    ///     Gets a value indicating whether the decoded entry must be flagged as sensitive
    ///     (never-indexed representation).
    /// </summary>
    public required bool IsSensitive { get; init; }

    /// <summary>
    ///     Gets the number of prefix bits used for the index integer (6 for incremental
    ///     indexing, 4 for never-indexed and without-indexing).
    /// </summary>
    public required int PrefixBits { get; init; }
}
