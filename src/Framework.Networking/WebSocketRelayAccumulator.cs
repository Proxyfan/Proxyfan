using System;
using System.Buffers;

namespace Proxyfan.Framework.Networking;

/// <summary>
///     Growable pooled byte buffer used by <see cref="WebSocketRelay" /> to accumulate
///     network bytes across reads without copying already-buffered data on every iteration.
///     Bytes are consumed in FIFO order: callers append to the tail with
///     <see cref="AdvanceWritten" /> after writing into the region exposed by
///     <see cref="EnsureWritableTail" />, and advance the parse cursor with
///     <see cref="AdvanceConsumed" />. The underlying array is compacted in-place when the
///     tail no longer has room, and grown only when even compaction is insufficient.
/// </summary>
internal sealed class WebSocketRelayAccumulator : IDisposable
{
    private readonly ArrayPool<byte> _pool;
    private byte[] _buffer;
    private int _consumedOffset;
    private int _writtenOffset;

    /// <summary>
    ///     The contiguous unconsumed bytes, ready to be parsed.
    /// </summary>
    public ReadOnlyMemory<byte> UnconsumedMemory => _buffer.AsMemory(_consumedOffset, _writtenOffset - _consumedOffset);

    /// <summary>
    ///     Initializes a new <see cref="WebSocketRelayAccumulator" /> backed by a rented
    ///     pooled buffer with at least <paramref name="initialCapacity" /> bytes.
    /// </summary>
    /// <param name="initialCapacity">Minimum initial capacity in bytes.</param>
    public WebSocketRelayAccumulator(int initialCapacity)
    {
        _pool = ArrayPool<byte>.Shared;
        _buffer = _pool.Rent(initialCapacity);
        _consumedOffset = 0;
        _writtenOffset = 0;
    }

    /// <summary>
    ///     Returns the disposed pooled buffer to the array pool.
    /// </summary>
    public void Dispose()
    {
        if (_buffer.Length > 0)
        {
            _pool.Return(_buffer);
            _buffer = [];
        }
    }

    /// <summary>
    ///     Marks <paramref name="count" /> additional bytes at the head of the unconsumed
    ///     region as parsed; they will not be exposed again.
    /// </summary>
    /// <param name="count">Number of bytes to mark consumed.</param>
    public void AdvanceConsumed(int count)
    {
        _consumedOffset += count;
        if (_consumedOffset == _writtenOffset)
        {
            _consumedOffset = 0;
            _writtenOffset = 0;
        }
    }

    /// <summary>
    ///     Marks <paramref name="count" /> additional bytes at the tail as freshly written;
    ///     these bytes will appear in <see cref="UnconsumedMemory" />.
    /// </summary>
    /// <param name="count">Number of bytes appended to the writable tail.</param>
    public void AdvanceWritten(int count)
    {
        _writtenOffset += count;
    }

    /// <summary>
    ///     Returns the latest <paramref name="count" /> appended bytes (the slice most
    ///     recently passed via <see cref="AdvanceWritten" />), suitable for forwarding to
    ///     the destination stream without re-copying.
    /// </summary>
    /// <param name="count">Number of trailing bytes to expose.</param>
    /// <returns>The trailing slice of the underlying buffer.</returns>
    public ReadOnlyMemory<byte> AsTailMemory(int count)
    {
        return _buffer.AsMemory(_writtenOffset - count, count);
    }

    /// <summary>
    ///     Ensures the writable tail has at least <paramref name="minimumTail" /> bytes,
    ///     compacting the unconsumed region in-place or growing the underlying buffer as
    ///     needed.
    /// </summary>
    /// <param name="minimumTail">Minimum required free bytes at the tail.</param>
    /// <returns>The writable tail region (at least <paramref name="minimumTail" /> bytes long).</returns>
    public Memory<byte> EnsureWritableTail(int minimumTail)
    {
        var tail = _buffer.Length - _writtenOffset;
        if (tail >= minimumTail)
        {
            return _buffer.AsMemory(_writtenOffset);
        }

        var unconsumed = _writtenOffset - _consumedOffset;

        if (_consumedOffset > 0 && _buffer.Length - unconsumed >= minimumTail)
        {
            if (unconsumed > 0)
            {
                Buffer.BlockCopy(_buffer, _consumedOffset, _buffer, 0, unconsumed);
            }

            _consumedOffset = 0;
            _writtenOffset = unconsumed;
            return _buffer.AsMemory(_writtenOffset);
        }

        var requiredCapacity = checked(unconsumed + minimumTail);
        var grown = _pool.Rent(Math.Max(requiredCapacity, _buffer.Length * 2));
        if (unconsumed > 0)
        {
            Buffer.BlockCopy(_buffer, _consumedOffset, grown, 0, unconsumed);
        }

        _pool.Return(_buffer);
        _buffer = grown;
        _consumedOffset = 0;
        _writtenOffset = unconsumed;
        return _buffer.AsMemory(_writtenOffset);
    }
}
