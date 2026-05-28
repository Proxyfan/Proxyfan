using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Proxyfan.Framework.Networking.Tests.Stubs;

/// <summary>
///     Hand-written stub stream that throws the supplied exceptions on read and/or write.
///     Used to exercise the IOException / SocketException catch branches in
///     <see cref="BidirectionalStreamPump" /> without relying on real socket failures.
/// </summary>
internal sealed class ThrowingStream : Stream
{
    private readonly Exception? _throwOnRead;
    private readonly Exception? _throwOnWrite;

    public ThrowingStream(Exception? throwOnRead = null, Exception? throwOnWrite = null)
    {
        _throwOnRead = throwOnRead;
        _throwOnWrite = throwOnWrite;
    }

    public override bool CanRead => true;
    public override bool CanSeek => false;
    public override bool CanWrite => true;
    public override long Length => throw new NotSupportedException();

    public override long Position
    {
        get { throw new NotSupportedException(); }
        set { throw new NotSupportedException(); }
    }

    public override void Flush()
    {
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        if (_throwOnRead is not null)
        {
            throw _throwOnRead;
        }

        return 0;
    }

    public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        if (_throwOnRead is not null)
        {
            throw _throwOnRead;
        }

        return ValueTask.FromResult(0);
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        throw new NotSupportedException();
    }

    public override void SetLength(long value)
    {
        throw new NotSupportedException();
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        if (_throwOnWrite is not null)
        {
            throw _throwOnWrite;
        }
    }

    public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
    {
        if (_throwOnWrite is not null)
        {
            throw _throwOnWrite;
        }

        return ValueTask.CompletedTask;
    }
}
