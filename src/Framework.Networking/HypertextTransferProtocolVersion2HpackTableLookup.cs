namespace Proxyfan.Framework.Networking;

/// <summary>
///     The result of an HPACK table lookup. <see cref="Index" /> is 0 when no entry matches the
///     supplied name. <see cref="IsExactMatch" /> is <c>true</c> only when both the name and the
///     value matched an entry — when just the name matches, the index still identifies a
///     candidate for literal-with-incremental-indexing encoding.
/// </summary>
public readonly record struct HypertextTransferProtocolVersion2HpackTableLookup
{
    /// <summary>
    ///     Gets the 1-based table index of the match, or 0 when no match was found.
    /// </summary>
    public int Index { get; }

    /// <summary>
    ///     Gets a value indicating whether the value (in addition to the name) matched the
    ///     supplied lookup tuple.
    /// </summary>
    public bool IsExactMatch { get; }

    /// <summary>
    ///     Initializes a new HPACK table lookup result.
    /// </summary>
    /// <param name="index">The 1-based table index of the match, or 0 when no match was found.</param>
    /// <param name="isExactMatch">Whether the value also matched.</param>
    public HypertextTransferProtocolVersion2HpackTableLookup(int index, bool isExactMatch)
    {
        Index = index;
        IsExactMatch = isExactMatch;
    }
}
