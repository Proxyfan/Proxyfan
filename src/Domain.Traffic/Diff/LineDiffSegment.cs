namespace Proxyfan.Domain.Traffic.Diff;

/// <summary>
///     A single, atomic segment produced by the <see cref="LineDiffer" />, representing
///     one line of source text and how it compares between the old and new inputs.
/// </summary>
public readonly record struct LineDiffSegment
{
    /// <summary>
    ///     Gets the one-based line number in the new text, or <c>null</c> when the
    ///     operation is <see cref="LineDiffOperation.Delete" />.
    /// </summary>
    public int? NewLineNumber { get; init; }

    /// <summary>
    ///     Gets the one-based line number in the old text, or <c>null</c> when the
    ///     operation is <see cref="LineDiffOperation.Insert" />.
    /// </summary>
    public int? OldLineNumber { get; init; }

    /// <summary>
    ///     Gets the diff operation classifying this segment.
    /// </summary>
    public LineDiffOperation Operation { get; init; }

    /// <summary>
    ///     Gets the textual content of the line, with no trailing line terminator.
    /// </summary>
    public required string Text { get; init; }
}
