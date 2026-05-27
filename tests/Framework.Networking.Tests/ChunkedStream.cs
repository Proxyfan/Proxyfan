using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Proxyfan.Framework.Networking.Tests;

/// <summary>
///     Stream that returns its source byte arrays one chunk per <see cref="ReadAsync(Memory{byte}, CancellationToken)" />
///     call. Used to validate parsers and relays that must reassemble data split across
///     transport boundaries.
/// </summary>
public sealed class ChunkedStream : Stream
{
    private readonly byte[][] _chunks;
    private int _chunkIndex;
    private int _offsetInChunk;

    /// <summary>
    ///     Gets a value indicating whether the stream supports reading.
    /// </summary>
    public override bool CanRead => true;

    /// <summary>
    ///     Gets a value indicating whether the stream supports seeking.
    /// </summary>
    public override bool CanSeek => false;

    /// <summary>
    ///     Gets a value indicating whether the stream supports writing.
    /// </summary>
    public override bool CanWrite => false;

    /// <summary>
    ///     Gets the total length of all chunks combined.
    /// </summary>
    public override long Length
    {
        get
        {
            var total = 0L;
            for (var index = 0; index < _chunks.Length; index++)
            {
                total += _chunks[index].Length;
            }
            return total;
        }
    }

    /// <summary>
    ///     Throws because the stream is not seekable.
    /// </summary>
    public override long Position
    {
        get => throw new NotSupportedException();
        set => throw new NotSupportedException();
    }

    /// <summary>
    ///     Initializes a new <see cref="ChunkedStream" /> from the supplied byte arrays. Each
    ///     array is returned as a single read.
    /// </summary>
    /// <param name="chunks">The chunks to return one per read.</param>
    public ChunkedStream(params byte[][] chunks)
    {
        _chunks = chunks;
        _chunkIndex = 0;
        _offsetInChunk = 0;
    }

    /// <summary>
    ///     No-op flush.
    /// </summary>
    public override void Flush()
    {
    }

    /// <summary>
    ///     Reads from the current chunk. Returns zero when all chunks are exhausted.
    /// </summary>
    /// <param name="buffer">Destination buffer.</param>
    /// <param name="offset">Destination offset.</param>
    /// <param name="count">Maximum bytes to read.</param>
    /// <returns>The number of bytes copied.</returns>
    public override int Read(byte[] buffer, int offset, int count)
    {
        if (_chunkIndex >= _chunks.Length)
        {
            return 0;
        }

        var currentChunk = _chunks[_chunkIndex];
        var available = currentChunk.Length - _offsetInChunk;
        var toCopy = count < available ? count : available;
        Array.Copy(currentChunk, _offsetInChunk, buffer, offset, toCopy);
        _offsetInChunk += toCopy;
        if (_offsetInChunk >= currentChunk.Length)
        {
            _chunkIndex++;
            _offsetInChunk = 0;
        }
        return toCopy;
    }

    /// <summary>
    ///     Asynchronous wrapper around <see cref="Read(byte[], int, int)" />.
    /// </summary>
    /// <param name="buffer">Destination buffer.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The number of bytes copied.</returns>
    public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        if (_chunkIndex >= _chunks.Length)
        {
            return ValueTask.FromResult(0);
        }

        var currentChunk = _chunks[_chunkIndex];
        var available = currentChunk.Length - _offsetInChunk;
        var toCopy = buffer.Length < available ? buffer.Length : available;
        currentChunk.AsMemory(_offsetInChunk, toCopy).CopyTo(buffer);
        _offsetInChunk += toCopy;
        if (_offsetInChunk >= currentChunk.Length)
        {
            _chunkIndex++;
            _offsetInChunk = 0;
        }
        return ValueTask.FromResult(toCopy);
    }

    /// <summary>
    ///     Throws because the stream is not seekable.
    /// </summary>
    /// <param name="offset">Ignored.</param>
    /// <param name="origin">Ignored.</param>
    /// <returns>Never returns.</returns>
    public override long Seek(long offset, SeekOrigin origin)
    {
        throw new NotSupportedException();
    }

    /// <summary>
    ///     Throws because the stream is not writable.
    /// </summary>
    /// <param name="value">Ignored.</param>
    public override void SetLength(long value)
    {
        throw new NotSupportedException();
    }

    /// <summary>
    ///     Throws because the stream is not writable.
    /// </summary>
    /// <param name="buffer">Ignored.</param>
    /// <param name="offset">Ignored.</param>
    /// <param name="count">Ignored.</param>
    public override void Write(byte[] buffer, int offset, int count)
    {
        throw new NotSupportedException();
    }
}
