namespace Proxyfan.Domain.Traffic.Diff;

/// <summary>
///     Classifies a single line within a <see cref="LineDiffSegment" /> as either
///     unchanged, added (present only in the new text), or removed (present only in
///     the old text).
/// </summary>
public enum LineDiffOperation
{
    /// <summary>
    ///     The line appears identically in both the old and new texts.
    /// </summary>
    Equal = 0,

    /// <summary>
    ///     The line is present in the new text but absent from the old text.
    /// </summary>
    Insert = 1,

    /// <summary>
    ///     The line is present in the old text but absent from the new text.
    /// </summary>
    Delete = 2,
}
