using System.Buffers;
using System.IO.Pipelines;
using System.Threading.Tasks;

namespace Proxyfan.Framework.Networking.Tests;

/// <summary>
///     Tests for <see cref="SocksHandshakeReader" />.
/// </summary>
public sealed class SocksHandshakeReaderTests
{
    /// <summary>
    ///     Verifies the version byte is detected without consuming it.
    /// </summary>
    [Test]
    public async Task DetectVersionAsync_Socks5_ReturnsFiveAndLeavesBufferIntact()
    {
        var pipe = new Pipe();
        await pipe.Writer.WriteAsync(new byte[] { 0x05, 0x01, 0x00 });
        await pipe.Writer.CompleteAsync();

        var version = await SocksHandshakeReader.DetectVersionAsync(pipe.Reader, default);

        await Assert.That(version).IsEqualTo(SocksVersion.Five);
        var result = await pipe.Reader.ReadAsync();
        await Assert.That((int)result.Buffer.Length).IsEqualTo(3);
    }

    /// <summary>
    ///     Verifies SOCKS4 version detection.
    /// </summary>
    [Test]
    public async Task DetectVersionAsync_Socks4_ReturnsFour()
    {
        var pipe = new Pipe();
        await pipe.Writer.WriteAsync(new byte[] { 0x04 });
        await pipe.Writer.CompleteAsync();

        var version = await SocksHandshakeReader.DetectVersionAsync(pipe.Reader, default);

        await Assert.That(version).IsEqualTo(SocksVersion.Four);
    }

    /// <summary>
    ///     Verifies that an empty pipe returns null.
    /// </summary>
    [Test]
    public async Task DetectVersionAsync_EmptyPipe_ReturnsNull()
    {
        var pipe = new Pipe();
        await pipe.Writer.CompleteAsync();

        var version = await SocksHandshakeReader.DetectVersionAsync(pipe.Reader, default);

        await Assert.That(version).IsNull();
    }

    /// <summary>
    ///     Verifies that an unknown protocol byte returns null.
    /// </summary>
    [Test]
    public async Task DetectVersionAsync_UnknownProtocol_ReturnsNull()
    {
        var pipe = new Pipe();
        await pipe.Writer.WriteAsync(new byte[] { 0x47 });
        await pipe.Writer.CompleteAsync();

        var version = await SocksHandshakeReader.DetectVersionAsync(pipe.Reader, default);

        await Assert.That(version).IsNull();
    }

    /// <summary>
    ///     Verifies that bytes are read into an array.
    /// </summary>
    [Test]
    public async Task ReadIntoArrayAsync_PipeWithFiveBytes_ReadsRequestedMinimum()
    {
        var pipe = new Pipe();
        await pipe.Writer.WriteAsync(new byte[] { 1, 2, 3, 4, 5 });
        await pipe.Writer.CompleteAsync();

        var bytes = await SocksHandshakeReader.ReadIntoArrayAsync(pipe.Reader, 3, 64, default);

        await Assert.That(bytes.Length).IsEqualTo(5);
        await Assert.That(bytes[0]).IsEqualTo((byte)1);
    }

    /// <summary>
    ///     Verifies the returned array is capped at maximumBytes even when the pipe has
    ///     buffered much more data (e.g. a pipelined payload following the handshake).
    /// </summary>
    [Test]
    public async Task ReadIntoArrayAsync_PipeWithPipelinedPayload_CapsAtMaximumBytes()
    {
        var pipe = new Pipe();
        var payload = new byte[8192];
        for (var index = 0; index < payload.Length; index++)
        {
            payload[index] = (byte)(index & 0xFF);
        }

        await pipe.Writer.WriteAsync(payload);
        await pipe.Writer.CompleteAsync();

        var bytes = await SocksHandshakeReader.ReadIntoArrayAsync(pipe.Reader, 2, 257, default);

        await Assert.That(bytes.Length).IsEqualTo(257);
        await Assert.That(bytes[0]).IsEqualTo(payload[0]);
        await Assert.That(bytes[256]).IsEqualTo(payload[256]);
        var result = await pipe.Reader.ReadAsync();
        await Assert.That((int)result.Buffer.Length).IsEqualTo(payload.Length);
    }

    /// <summary>
    ///     Verifies HasNoAuthMethod returns true when 0x00 is present.
    /// </summary>
    [Test]
    [Arguments(new byte[] { 0x00 }, true)]
    [Arguments(new byte[] { 0x02, 0x00 }, true)]
    [Arguments(new byte[] { 0x02 }, false)]
    [Arguments(new byte[] { }, false)]
    public async Task HasNoAuthMethod_VariousInputs_ReturnsExpected(byte[] methods, bool expected)
    {
        var result = SocksHandshakeReader.HasNoAuthMethod(methods);

        await Assert.That(result).IsEqualTo(expected);
    }
}
