using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Proxyfan.Framework.Networking;

/// <summary>
///     A read-through <see cref="Stream" /> wrapper that delivers a buffered prefix of bytes
///     before reading from the underlying stream. Writes, flushes, and disposal pass straight
///     through to the inner stream. The prefix exists so that bytes read ahead during HTTP
///     header parsing (for example, the first WebSocket frame that arrived alongside the 101
///     Switching Protocols response) are not lost when the tunnel takes over the connection.
/// </summary>
public sealed class PrefixedReadStream : Stream
{
    private readonly Stream _inner;
    private readonly byte[] _prefix;
    private int _prefixPosition;

    /// <summary>
    ///     Initializes a new <see cref="PrefixedReadStream" />.
    /// </summary>
    /// <param name="prefix">The bytes to deliver before reading from <paramref name="inner" />.</param>
    /// <param name="inner">The stream that supplies bytes after the prefix is exhausted.</param>
    public PrefixedReadStream(byte[] prefix, Stream inner)
    {
        _prefix = prefix;
        _inner = inner;
    }

    /// <inheritdoc />
    public override bool CanRead => _inner.CanRead;

    /// <inheritdoc />
    public override bool CanSeek => false;

    /// <inheritdoc />
    public override bool CanWrite => _inner.CanWrite;

    /// <inheritdoc />
    public override void Flush()
    {
        _inner.Flush();
    }

    /// <inheritdoc />
    public override Task FlushAsync(CancellationToken cancellationToken)
    {
        return _inner.FlushAsync(cancellationToken);
    }

    /// <inheritdoc />
    public override long Length => throw new NotSupportedException();

    /// <inheritdoc />
    public override long Position
    {
        get => throw new NotSupportedException();
        set => throw new NotSupportedException();
    }

    /// <inheritdoc />
    public override int Read(byte[] buffer, int offset, int count)
    {
        var prefixRemaining = _prefix.Length - _prefixPosition;

        if (prefixRemaining > 0)
        {
            var toCopy = Math.Min(prefixRemaining, count);
            Array.Copy(_prefix, _prefixPosition, buffer, offset, toCopy);
            _prefixPosition += toCopy;
            return toCopy;
        }

        return _inner.Read(buffer, offset, count);
    }

    /// <inheritdoc />
    public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        var prefixRemaining = _prefix.Length - _prefixPosition;

        if (prefixRemaining > 0)
        {
            var toCopy = Math.Min(prefixRemaining, buffer.Length);
            _prefix.AsMemory(_prefixPosition, toCopy).CopyTo(buffer);
            _prefixPosition += toCopy;
            return toCopy;
        }

        var result = await _inner.ReadAsync(buffer, cancellationToken).ConfigureAwait(false);
        return result;
    }

    /// <inheritdoc />
    public override long Seek(long offset, SeekOrigin origin)
    {
        throw new NotSupportedException();
    }

    /// <inheritdoc />
    public override void SetLength(long value)
    {
        throw new NotSupportedException();
    }

    /// <inheritdoc />
    public override void Write(byte[] buffer, int offset, int count)
    {
        _inner.Write(buffer, offset, count);
    }

    /// <inheritdoc />
    public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
    {
        return _inner.WriteAsync(buffer, cancellationToken);
    }

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _inner.Dispose();
        }

        base.Dispose(disposing);
    }
}
