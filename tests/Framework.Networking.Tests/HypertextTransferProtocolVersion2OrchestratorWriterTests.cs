using System;
using System.Threading.Tasks;

namespace Proxyfan.Framework.Networking.Tests;

/// <summary>
///     Tests for <see cref="HypertextTransferProtocolVersion2OrchestratorWriter" />.
/// </summary>
public sealed class HypertextTransferProtocolVersion2OrchestratorWriterTests
{
    /// <summary>
    ///     Verifies that a successful write yields <see langword="true" /> and the bytes land
    ///     in the destination stream.
    /// </summary>
    [Test]
    public async Task TryWriteFrameAsync_NormalStream_WritesAndReturnsTrue()
    {
        using var destination = new System.IO.MemoryStream();
        var frame = new byte[] { 1, 2, 3 };

        var ok = await HypertextTransferProtocolVersion2OrchestratorWriter.TryWriteFrameAsync(destination, frame, System.Threading.CancellationToken.None);

        await Assert.That(ok).IsTrue();
        await Assert.That(destination.ToArray()).IsEquivalentTo(frame);
    }

    /// <summary>
    ///     Verifies that an <see cref="System.ObjectDisposedException" /> from the destination
    ///     is swallowed and the helper returns <see langword="false" />.
    /// </summary>
    [Test]
    public async Task TryWriteFrameAsync_DisposedStream_ReturnsFalse()
    {
        var destination = new System.IO.MemoryStream();
        destination.Dispose();

        var ok = await HypertextTransferProtocolVersion2OrchestratorWriter.TryWriteFrameAsync(destination, new byte[] { 1 }, System.Threading.CancellationToken.None);

        await Assert.That(ok).IsFalse();
    }

    /// <summary>
    ///     Verifies that an <see cref="System.IO.IOException" /> from the destination is
    ///     swallowed and the helper returns <see langword="false" />.
    /// </summary>
    [Test]
    public async Task TryWriteFrameAsync_StreamThrowsIoException_ReturnsFalse()
    {
        using var destination = new ThrowingWriteStream(new System.IO.IOException("simulated"));

        var ok = await HypertextTransferProtocolVersion2OrchestratorWriter.TryWriteFrameAsync(destination, new byte[] { 1 }, System.Threading.CancellationToken.None);

        await Assert.That(ok).IsFalse();
    }

    /// <summary>
    ///     Verifies that <see cref="HypertextTransferProtocolVersion2OrchestratorWriter.TryForwardFrameAsync" />
    ///     serializes the supplied frame (header + payload) verbatim into the destination
    ///     while leasing the buffer from <see cref="System.Buffers.ArrayPool{T}.Shared" />.
    /// </summary>
    [Test]
    public async Task TryForwardFrameAsync_AnyFrame_WritesHeaderAndPayloadToDestination()
    {
        using var destination = new System.IO.MemoryStream();
        var payload = new byte[] { 0xAA, 0xBB, 0xCC, 0xDD };
        var header = new HypertextTransferProtocolVersion2FrameHeader(
            length: payload.Length,
            rawType: (byte)HypertextTransferProtocolVersion2FrameType.Data,
            flags: HypertextTransferProtocolVersion2FrameFlag.EndStreamOrAcknowledge,
            streamIdentifier: 7);
        var frame = new HypertextTransferProtocolVersion2Frame(header, payload);

        var ok = await HypertextTransferProtocolVersion2OrchestratorWriter.TryForwardFrameAsync(destination, frame, System.Threading.CancellationToken.None);

        await Assert.That(ok).IsTrue();
        var expected = new byte[HypertextTransferProtocolVersion2FrameParser.HeaderLength + payload.Length];
        var descriptor = HypertextTransferProtocolVersion2OrchestratorHelpers.BuildDescriptor(frame);
        HypertextTransferProtocolVersion2FrameWriter.WriteFrame(expected, descriptor, payload);
        await Assert.That(destination.ToArray()).IsEquivalentTo(expected);
    }

    private sealed class ThrowingWriteStream : System.IO.Stream
    {
        private readonly Exception _throwOnWrite;

        public ThrowingWriteStream(Exception throwOnWrite)
        {
            _throwOnWrite = throwOnWrite;
        }

        public override bool CanRead => false;

        public override bool CanSeek => false;

        public override bool CanWrite => true;

        public override long Length => throw new NotSupportedException();

        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public override void Flush()
        {
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }

        public override long Seek(long offset, System.IO.SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw _throwOnWrite;
        }

        public override System.Threading.Tasks.ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, System.Threading.CancellationToken cancellationToken)
        {
            throw _throwOnWrite;
        }
    }
}
