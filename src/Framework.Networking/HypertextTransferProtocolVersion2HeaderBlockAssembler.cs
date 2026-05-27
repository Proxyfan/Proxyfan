using System;
using System.Buffers;

namespace Proxyfan.Framework.Networking;

/// <summary>
///     Assembles HEADERS (and PUSH_PROMISE) header block fragments across one or more
///     CONTINUATION frames (RFC 7540 § 6.10). The assembler enforces:
///     <list type="bullet">
///       <item><description>Only one in-progress header block per connection at a time.</description></item>
///       <item><description>All CONTINUATION frames must belong to the in-progress stream.</description></item>
///       <item><description>The assembled fragment size must not exceed a caller-supplied cap (default 64 KB).</description></item>
///     </list>
///     Violations of these rules are reported via <c>null</c> return values rather than
///     exceptions to keep the hot path allocation-free.
/// </summary>
public sealed class HypertextTransferProtocolVersion2HeaderBlockAssembler
{
    /// <summary>
    ///     Default maximum assembled header block size (64 KB) per the recommendation in
    ///     RFC 7540 § 10.5.
    /// </summary>
    public const int DefaultMaximumByteSize = 65536;
    private readonly ArrayBufferWriter<byte> _buffer;
    private readonly int _maximumByteSize;
    private uint _activeStreamIdentifier;

    /// <summary>
    ///     Gets the current size of the in-progress header block fragment (0 when no block is in progress).
    /// </summary>
    public int CurrentByteSize => _buffer.WrittenCount;

    /// <summary>
    ///     Gets a value indicating whether a HEADERS/PUSH_PROMISE block is currently being
    ///     assembled and awaits its END_HEADERS-bearing CONTINUATION.
    /// </summary>
    public bool IsInProgress => _activeStreamIdentifier != 0;

    /// <summary>
    ///     Initializes a new assembler with the default 64 KB size cap.
    /// </summary>
    public HypertextTransferProtocolVersion2HeaderBlockAssembler()
        : this(DefaultMaximumByteSize)
    {
    }

    /// <summary>
    ///     Initializes a new assembler with an explicit maximum assembled-block size.
    /// </summary>
    /// <param name="maximumByteSize">The maximum number of bytes the assembled block may occupy.</param>
    public HypertextTransferProtocolVersion2HeaderBlockAssembler(int maximumByteSize)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(maximumByteSize, 0);
        _maximumByteSize = maximumByteSize;
        var buffer = new ArrayBufferWriter<byte>();
        _buffer = buffer;
    }

    /// <summary>
    ///     Appends a CONTINUATION fragment to the in-progress block. Returns the fully assembled
    ///     block when <paramref name="hasEndHeadersFlag" /> is set; otherwise <c>null</c>.
    /// </summary>
    /// <param name="streamIdentifier">The stream id from the CONTINUATION frame header (must match the active block).</param>
    /// <param name="fragment">The CONTINUATION payload.</param>
    /// <param name="hasEndHeadersFlag">Whether END_HEADERS was set on the CONTINUATION frame.</param>
    /// <returns>The completed block when END_HEADERS is set; otherwise <c>null</c>.</returns>
    public byte[]? AppendContinuation(uint streamIdentifier, ReadOnlySpan<byte> fragment, bool hasEndHeadersFlag)
    {
        if (!IsInProgress)
        {
            return null;
        }
        if (streamIdentifier != _activeStreamIdentifier)
        {
            Reset();
            return null;
        }
        if (_buffer.WrittenCount + fragment.Length > _maximumByteSize)
        {
            Reset();
            return null;
        }
        _buffer.Write(fragment);
        if (!hasEndHeadersFlag)
        {
            return null;
        }
        var complete = _buffer.WrittenSpan.ToArray();
        Reset();
        return complete;
    }

    /// <summary>
    ///     Begins assembling a new header block for <paramref name="streamIdentifier" /> and
    ///     appends the initial <paramref name="fragment" />. When <paramref name="hasEndHeadersFlag" />
    ///     is true the block is returned immediately; otherwise the caller must drive the
    ///     remaining fragments through <see cref="AppendContinuation" />.
    /// </summary>
    /// <param name="streamIdentifier">The HEADERS/PUSH_PROMISE stream id (must be non-zero).</param>
    /// <param name="fragment">The initial header block fragment.</param>
    /// <param name="hasEndHeadersFlag">Whether END_HEADERS was set on the originating frame.</param>
    /// <returns>
    ///     The fully assembled block when <paramref name="hasEndHeadersFlag" /> is true; <c>null</c>
    ///     when the block is incomplete and awaits CONTINUATION frames; or when a prior block was
    ///     already in progress (protocol violation).
    /// </returns>
    public byte[]? BeginBlock(uint streamIdentifier, ReadOnlySpan<byte> fragment, bool hasEndHeadersFlag)
    {
        if (streamIdentifier == 0 || IsInProgress)
        {
            return null;
        }
        if (fragment.Length > _maximumByteSize)
        {
            return null;
        }
        _buffer.ResetWrittenCount();
        _buffer.Write(fragment);
        if (hasEndHeadersFlag)
        {
            var complete = _buffer.WrittenSpan.ToArray();
            _buffer.ResetWrittenCount();
            return complete;
        }
        _activeStreamIdentifier = streamIdentifier;
        return null;
    }

    /// <summary>
    ///     Discards any in-progress block (used when the connection encounters a fatal error).
    /// </summary>
    public void Reset()
    {
        _buffer.ResetWrittenCount();
        _activeStreamIdentifier = 0;
    }
}
