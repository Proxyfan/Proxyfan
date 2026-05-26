using System;
using System.IO.Pipelines;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Proxyfan.Framework.Networking.Tests;

/// <summary>
///     Tests for <see cref="DuplexPipeStream" />.
/// </summary>
public sealed class DuplexPipeStreamTests
{
    /// <summary>
    ///     Verifies that the stream reports the expected capabilities.
    /// </summary>
    [Test]
    public async Task Capabilities_AfterConstruction_AreAsExpected()
    {
        var readPipe = new Pipe();
        var writePipe = new Pipe();
        using var stream = new DuplexPipeStream(readPipe.Reader, writePipe.Writer);

        await Assert.That(stream.CanRead).IsTrue();
        await Assert.That(stream.CanSeek).IsFalse();
        await Assert.That(stream.CanTimeout).IsFalse();
        await Assert.That(stream.CanWrite).IsTrue();
    }

    /// <summary>
    ///     Verifies that <see cref="DuplexPipeStream.Length" /> throws <see cref="NotSupportedException" />.
    /// </summary>
    [Test]
    public async Task Length_AfterConstruction_Throws()
    {
        var readPipe = new Pipe();
        var writePipe = new Pipe();
        using var stream = new DuplexPipeStream(readPipe.Reader, writePipe.Writer);

        await Assert.That(() => _ = stream.Length).Throws<NotSupportedException>();
    }

    /// <summary>
    ///     Verifies that the <see cref="DuplexPipeStream.Position" /> getter throws.
    /// </summary>
    [Test]
    public async Task GetPosition_AfterConstruction_Throws()
    {
        var readPipe = new Pipe();
        var writePipe = new Pipe();
        using var stream = new DuplexPipeStream(readPipe.Reader, writePipe.Writer);

        await Assert.That(() => _ = stream.Position).Throws<NotSupportedException>();
    }

    /// <summary>
    ///     Verifies that the <see cref="DuplexPipeStream.Position" /> setter throws.
    /// </summary>
    [Test]
    public async Task SetPosition_AfterConstruction_Throws()
    {
        var readPipe = new Pipe();
        var writePipe = new Pipe();
        using var stream = new DuplexPipeStream(readPipe.Reader, writePipe.Writer);

        await Assert.That(() => stream.Position = 5).Throws<NotSupportedException>();
    }

    /// <summary>
    ///     Verifies that <see cref="DuplexPipeStream.Seek" /> throws.
    /// </summary>
    [Test]
    public async Task Seek_AnyOffset_Throws()
    {
        var readPipe = new Pipe();
        var writePipe = new Pipe();
        using var stream = new DuplexPipeStream(readPipe.Reader, writePipe.Writer);

        await Assert.That(() => stream.Seek(0, System.IO.SeekOrigin.Begin)).Throws<NotSupportedException>();
    }

    /// <summary>
    ///     Verifies that <see cref="DuplexPipeStream.SetLength" /> throws.
    /// </summary>
    [Test]
    public async Task SetLength_AnyValue_Throws()
    {
        var readPipe = new Pipe();
        var writePipe = new Pipe();
        using var stream = new DuplexPipeStream(readPipe.Reader, writePipe.Writer);

        await Assert.That(() => stream.SetLength(100)).Throws<NotSupportedException>();
    }

    /// <summary>
    ///     Verifies that writing bytes via the async API flows to the underlying writer.
    /// </summary>
    [Test]
    public async Task WriteAsync_ThenFlush_DeliversToUnderlyingPipe()
    {
        var readPipe = new Pipe();
        var writePipe = new Pipe();
        await using var stream = new DuplexPipeStream(readPipe.Reader, writePipe.Writer);
        var payload = Encoding.ASCII.GetBytes("hello-write");

        await stream.WriteAsync(payload, CancellationToken.None);
        await stream.FlushAsync(CancellationToken.None);
        await writePipe.Writer.CompleteAsync();

        using var memoryStream = new System.IO.MemoryStream();
        await writePipe.Reader.AsStream().CopyToAsync(memoryStream);
        var written = memoryStream.ToArray();
        await Assert.That(Encoding.ASCII.GetString(written)).IsEqualTo("hello-write");
    }

    /// <summary>
    ///     Verifies that synchronous Write+Flush also delivers to the underlying writer.
    /// </summary>
    [Test]
    public async Task Write_SynchronousAPI_DeliversBytes()
    {
        var readPipe = new Pipe();
        var writePipe = new Pipe();
        await using var stream = new DuplexPipeStream(readPipe.Reader, writePipe.Writer);
        var payload = Encoding.ASCII.GetBytes("sync-write");

        stream.Write(payload, 0, payload.Length);
        stream.Flush();
        await writePipe.Writer.CompleteAsync();

        using var memoryStream = new System.IO.MemoryStream();
        await writePipe.Reader.AsStream().CopyToAsync(memoryStream);
        var written = memoryStream.ToArray();
        await Assert.That(Encoding.ASCII.GetString(written)).IsEqualTo("sync-write");
    }

    /// <summary>
    ///     Verifies that reading bytes returns what was written to the source pipe.
    /// </summary>
    [Test]
    public async Task ReadAsync_FromCompletedPipe_ReturnsExpectedBytes()
    {
        var readPipe = new Pipe();
        var writePipe = new Pipe();
        await using var stream = new DuplexPipeStream(readPipe.Reader, writePipe.Writer);
        var payload = Encoding.ASCII.GetBytes("inbound");

        await readPipe.Writer.WriteAsync(payload);
        await readPipe.Writer.CompleteAsync();

        var buffer = new byte[16];
        var bytesRead = await stream.ReadAsync(buffer, CancellationToken.None);

        await Assert.That(bytesRead).IsEqualTo(payload.Length);
        await Assert.That(Encoding.ASCII.GetString(buffer, 0, bytesRead)).IsEqualTo("inbound");
    }

    /// <summary>
    ///     Verifies that synchronous Read returns what was written to the source pipe.
    /// </summary>
    [Test]
    public async Task Read_SynchronousAPI_ReturnsExpectedBytes()
    {
        var readPipe = new Pipe();
        var writePipe = new Pipe();
        await using var stream = new DuplexPipeStream(readPipe.Reader, writePipe.Writer);
        var payload = Encoding.ASCII.GetBytes("sync-read");

        await readPipe.Writer.WriteAsync(payload);
        await readPipe.Writer.CompleteAsync();

        var buffer = new byte[16];
        var bytesRead = stream.Read(buffer, 0, buffer.Length);

        await Assert.That(bytesRead).IsEqualTo(payload.Length);
        await Assert.That(Encoding.ASCII.GetString(buffer, 0, bytesRead)).IsEqualTo("sync-read");
    }
}
