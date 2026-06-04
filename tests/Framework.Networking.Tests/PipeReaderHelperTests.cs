using System;
using System.IO.Pipelines;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Proxyfan.Framework.Networking.Tests;

/// <summary>
///     Tests for <see cref="PipeReaderHelper" />.
/// </summary>
public sealed class PipeReaderHelperTests
{
    /// <summary>
    ///     Verifies that when end-of-headers is found in the buffer but the resulting header
    ///     length exceeds <paramref name="maxBytes" /> the method returns null and the buffer
    ///     is advanced to the end.
    /// </summary>
    [Test]
    public async Task ReadUntilEndOfHeadersAsync_EndFoundButHeaderLengthExceedsMaxBytes_ReturnsNull()
    {
        var pipe = new Pipe();
        var headers = new string('X', 60) + "\r\n\r\n";
        await pipe.Writer.WriteAsync(Encoding.ASCII.GetBytes(headers));
        await pipe.Writer.FlushAsync();

        var result = await PipeReaderHelper.ReadUntilEndOfHeadersAsync(
            pipe.Reader, 50, CancellationToken.None);

        await Assert.That(result).IsNull();
    }

    /// <summary>
    ///     Verifies that reading headers with a valid CRLF-terminated block returns all header bytes.
    /// </summary>
    [Test]
    public async Task ReadUntilEndOfHeadersAsync_ValidHeaders_ReturnsHeaderBytes()
    {
        var pipe = new Pipe();
        const string headers = "GET / HTTP/1.1\r\nHost: example.com\r\n\r\n";
        var bytes = Encoding.ASCII.GetBytes(headers);
        await pipe.Writer.WriteAsync(bytes);
        await pipe.Writer.FlushAsync();

        var result = await PipeReaderHelper.ReadUntilEndOfHeadersAsync(
            pipe.Reader, 65536, CancellationToken.None);

        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Length).IsEqualTo(bytes.Length);
    }

    /// <summary>
    ///     Verifies that when headers arrive in multiple small chunks, the method waits
    ///     and assembles the full headers once the end sequence is found.
    /// </summary>
    [Test]
    public async Task ReadUntilEndOfHeadersAsync_HeadersInMultipleChunks_ReturnsAssembledBytes()
    {
        var pipe = new Pipe();
        const string part1 = "GET / HTTP/1.1\r\n";
        const string part2 = "Host: example.com\r\n\r\n";
        await Assert.That(part1.Contains("\r\n\r\n", StringComparison.Ordinal)).IsFalse();

        await pipe.Writer.WriteAsync(Encoding.ASCII.GetBytes(part1));
        await pipe.Writer.FlushAsync();

        var readTask = PipeReaderHelper.ReadUntilEndOfHeadersAsync(
            pipe.Reader, 65536, CancellationToken.None);

        await pipe.Writer.WriteAsync(Encoding.ASCII.GetBytes(part2));
        await pipe.Writer.CompleteAsync();

        var result = await readTask;

        await Assert.That(result).IsNotNull();
        var expectedLength = Encoding.ASCII.GetByteCount(part1 + part2);
        await Assert.That(result!.Length).IsEqualTo(expectedLength);
    }

    /// <summary>
    ///     Verifies that when accumulated bytes exceed the max limit, the method returns null.
    /// </summary>
    [Test]
    public async Task ReadUntilEndOfHeadersAsync_ExceedsMaxBytes_ReturnsNull()
    {
        var pipe = new Pipe();
        var largeHeaders = new string('X', 100) + "\r\nValue: " + new string('Y', 200) + "\r\n";
        await pipe.Writer.WriteAsync(Encoding.ASCII.GetBytes(largeHeaders));
        await pipe.Writer.FlushAsync();
        await pipe.Writer.CompleteAsync();

        var result = await PipeReaderHelper.ReadUntilEndOfHeadersAsync(
            pipe.Reader, 50, CancellationToken.None);

        await Assert.That(result).IsNull();
    }

    /// <summary>
    ///     Verifies that when the pipe is completed before end-of-headers is found, the method returns null.
    /// </summary>
    [Test]
    public async Task ReadUntilEndOfHeadersAsync_PipeCompletedWithoutEndOfHeaders_ReturnsNull()
    {
        var pipe = new Pipe();
        await pipe.Writer.WriteAsync(Encoding.ASCII.GetBytes("GET / HTTP/1.1\r\n"));
        await pipe.Writer.FlushAsync();
        await pipe.Writer.CompleteAsync();

        var result = await PipeReaderHelper.ReadUntilEndOfHeadersAsync(
            pipe.Reader, 65536, CancellationToken.None);

        await Assert.That(result).IsNull();
    }

    /// <summary>
    ///     Verifies that cancellation before end-of-headers causes an OperationCanceledException.
    /// </summary>
    [Test]
    public async Task ReadUntilEndOfHeadersAsync_CancelledBeforeEndOfHeaders_ThrowsOperationCanceledException()
    {
        var pipe = new Pipe();

        using var cancellationSource = new CancellationTokenSource();
        cancellationSource.CancelAfter(TimeSpan.FromMilliseconds(30));

        await Assert.That(async () =>
            await PipeReaderHelper.ReadUntilEndOfHeadersAsync(
                pipe.Reader, 65536, cancellationSource.Token)
        ).Throws<OperationCanceledException>();
    }

    /// <summary>
    ///     Verifies that when the end-of-headers sequence appears immediately, it is detected correctly.
    /// </summary>
    [Test]
    public async Task ReadUntilEndOfHeadersAsync_OnlyEndOfHeadersSequence_ReturnsBytes()
    {
        var pipe = new Pipe();
        var endOfHeadersSequence = "\r\n\r\n"u8.ToArray();
        await pipe.Writer.WriteAsync(endOfHeadersSequence);
        await pipe.Writer.FlushAsync();

        var result = await PipeReaderHelper.ReadUntilEndOfHeadersAsync(
            pipe.Reader, 65536, CancellationToken.None);

        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Length).IsEqualTo(4);
    }
}
