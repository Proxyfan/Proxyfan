using System;
using System.IO;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;

namespace Proxyfan.Framework.Networking;

/// <summary>
///     A bidirectional <see cref="Stream" /> that reads from a <see cref="PipeReader" /> and
///     writes to a <see cref="PipeWriter" />. Used by the TLS interceptor to bridge between
///     the proxy connection's duplex pipe and the <see cref="System.Net.Security.SslStream" />.
/// </summary>
public sealed class DuplexPipeStream : Stream
{
    private readonly Stream _readStream;
    private readonly Stream _writeStream;

    /// <summary>
    ///     Initializes a new <see cref="DuplexPipeStream" /> wrapping the supplied reader and writer.
    /// </summary>
    /// <param name="reader">The pipe reader to read from.</param>
    /// <param name="writer">The pipe writer to write to.</param>
    public DuplexPipeStream(PipeReader reader, PipeWriter writer)
    {
        var readStream = reader.AsStream();
        var writeStream = writer.AsStream();
        _readStream = readStream;
        _writeStream = writeStream;
    }

    /// <inheritdoc />
    public override bool CanRead => true;

    /// <inheritdoc />
    public override bool CanSeek => false;

    /// <inheritdoc />
    public override bool CanTimeout => false;

    /// <inheritdoc />
    public override bool CanWrite => true;

    /// <inheritdoc />
    public override void Flush()
    {
        _writeStream.Flush();
    }

    /// <inheritdoc />
    public override Task FlushAsync(CancellationToken cancellationToken)
    {
        return _writeStream.FlushAsync(cancellationToken);
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
        return _readStream.Read(buffer, offset, count);
    }

    /// <inheritdoc />
    public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        var bytesRead = await _readStream.ReadAsync(buffer, cancellationToken).ConfigureAwait(false);
        return bytesRead;
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
        _writeStream.Write(buffer, offset, count);
    }

    /// <inheritdoc />
    public override async ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
    {
        await _writeStream.WriteAsync(buffer, cancellationToken).ConfigureAwait(false);
    }
}
