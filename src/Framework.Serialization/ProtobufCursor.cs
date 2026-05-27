namespace Proxyfan.Framework.Serialization;

/// <summary>
///     Mutable byte-stream cursor used by <see cref="ProtobufDecoder" /> to track the current
///     read offset within a buffer. Designed to avoid <c>ref</c> parameters (forbidden by
///     ATXCS025/026 in this codebase) while still allowing per-call state advancement.
/// </summary>
public sealed class ProtobufCursor
{
    /// <summary>
    ///     Gets the current read offset.
    /// </summary>
    public int Offset { get; private set; }

    /// <summary>
    ///     Initializes a new <see cref="ProtobufCursor" /> at the supplied offset.
    /// </summary>
    /// <param name="initialOffset">The initial read offset (typically zero).</param>
    public ProtobufCursor(int initialOffset)
    {
        Offset = initialOffset;
    }

    /// <summary>
    ///     Advances the cursor by the supplied number of bytes.
    /// </summary>
    /// <param name="count">The number of bytes to advance.</param>
    public void Advance(int count)
    {
        Offset += count;
    }
}
