using System.Buffers;
using System.IO.Pipelines;
using System.Threading.Tasks;

namespace Proxyfan.Framework.Networking.Tests;

/// <summary>
///     Tests for <see cref="SocksReplyWriter" />.
/// </summary>
public sealed class SocksReplyWriterTests
{
    /// <summary>
    ///     Verifies SOCKS4 success reply emits 0x00, 0x5A, then six zero padding bytes.
    /// </summary>
    [Test]
    public async Task WriteSocks4ReplyAsync_Success_WritesGrantedReply()
    {
        var pipe = new Pipe();
        await SocksReplyWriter.WriteSocks4ReplyAsync(pipe.Writer, isSuccess: true, default);
        await pipe.Writer.CompleteAsync();
        var bytes = await ReadAllAsync(pipe.Reader);

        await Assert.That(bytes.Length).IsEqualTo(8);
        await Assert.That(bytes[0]).IsEqualTo((byte)0x00);
        await Assert.That(bytes[1]).IsEqualTo((byte)0x5A);
    }

    /// <summary>
    ///     Verifies SOCKS4 failure reply uses 0x5B.
    /// </summary>
    [Test]
    public async Task WriteSocks4ReplyAsync_Failure_WritesRejectedReply()
    {
        var pipe = new Pipe();
        await SocksReplyWriter.WriteSocks4ReplyAsync(pipe.Writer, isSuccess: false, default);
        await pipe.Writer.CompleteAsync();
        var bytes = await ReadAllAsync(pipe.Reader);

        await Assert.That(bytes[1]).IsEqualTo((byte)0x5B);
    }

    /// <summary>
    ///     Verifies SOCKS5 success reply uses REP=0x00 with IPv4 zero ATYP padding.
    /// </summary>
    [Test]
    public async Task WriteSocks5SuccessReplyAsync_DefaultPayload_WritesTenByteReply()
    {
        var pipe = new Pipe();
        await SocksReplyWriter.WriteSocks5SuccessReplyAsync(pipe.Writer, default);
        await pipe.Writer.CompleteAsync();
        var bytes = await ReadAllAsync(pipe.Reader);

        await Assert.That(bytes.Length).IsEqualTo(10);
        await Assert.That(bytes[0]).IsEqualTo((byte)0x05);
        await Assert.That(bytes[1]).IsEqualTo((byte)0x00);
        await Assert.That(bytes[3]).IsEqualTo((byte)0x01);
    }

    /// <summary>
    ///     Verifies SOCKS5 failure reply uses REP=0x05.
    /// </summary>
    [Test]
    public async Task WriteSocks5FailureReplyAsync_DefaultPayload_WritesReplyWithFailureCode()
    {
        var pipe = new Pipe();
        await SocksReplyWriter.WriteSocks5FailureReplyAsync(pipe.Writer, default);
        await pipe.Writer.CompleteAsync();
        var bytes = await ReadAllAsync(pipe.Reader);

        await Assert.That(bytes[1]).IsEqualTo((byte)0x05);
    }

    private static async Task<byte[]> ReadAllAsync(PipeReader reader)
    {
        var result = await reader.ReadAsync();
        var bytes = result.Buffer.ToArray();
        reader.AdvanceTo(result.Buffer.End);
        await reader.CompleteAsync();
        return bytes;
    }
}
