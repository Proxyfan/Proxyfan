using System.IO.Pipelines;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Proxyfan.Framework.Networking.Tests;

/// <summary>
///     Tests for <see cref="HypertextTransferProtocolChunkedBodyReader" /> per RFC 7230 § 4.1.
/// </summary>
public sealed class HypertextTransferProtocolChunkedBodyReaderTests
{
    /// <summary>
    ///     A single non-empty chunk followed by the terminating zero chunk yields the chunk data.
    /// </summary>
    [Test]
    public async Task ReadAsync_SingleChunk_ReturnsChunkBody()
    {
        var pipe = await WriteToPipeAsync("5\r\nhello\r\n0\r\n\r\n");

        var body = await HypertextTransferProtocolChunkedBodyReader.ReadAsync(pipe.Reader, CancellationToken.None);

        await Assert.That(body).IsNotNull();
        await Assert.That(Encoding.ASCII.GetString(body!)).IsEqualTo("hello");
    }

    /// <summary>
    ///     Multiple chunks are concatenated in order to form the decoded body.
    /// </summary>
    [Test]
    public async Task ReadAsync_MultipleChunks_ConcatenatesBodies()
    {
        var pipe = await WriteToPipeAsync("5\r\nhello\r\n7\r\n, world\r\n0\r\n\r\n");

        var body = await HypertextTransferProtocolChunkedBodyReader.ReadAsync(pipe.Reader, CancellationToken.None);

        await Assert.That(body).IsNotNull();
        await Assert.That(Encoding.ASCII.GetString(body!)).IsEqualTo("hello, world");
    }

    /// <summary>
    ///     A terminating zero chunk with no preceding data yields an empty body.
    /// </summary>
    [Test]
    public async Task ReadAsync_OnlyTerminatingChunk_ReturnsEmpty()
    {
        var pipe = await WriteToPipeAsync("0\r\n\r\n");

        var body = await HypertextTransferProtocolChunkedBodyReader.ReadAsync(pipe.Reader, CancellationToken.None);

        await Assert.That(body).IsNotNull();
        await Assert.That(body!.Length).IsEqualTo(0);
    }

    /// <summary>
    ///     Chunk extensions (after the semicolon on the size line) are accepted but ignored.
    /// </summary>
    [Test]
    public async Task ReadAsync_ChunkWithExtension_IgnoresExtension()
    {
        var pipe = await WriteToPipeAsync("5;name=value\r\nhello\r\n0\r\n\r\n");

        var body = await HypertextTransferProtocolChunkedBodyReader.ReadAsync(pipe.Reader, CancellationToken.None);

        await Assert.That(body).IsNotNull();
        await Assert.That(Encoding.ASCII.GetString(body!)).IsEqualTo("hello");
    }

    /// <summary>
    ///     Trailer headers after the terminating chunk are accepted but discarded.
    /// </summary>
    [Test]
    public async Task ReadAsync_WithTrailers_IgnoresTrailers()
    {
        var pipe = await WriteToPipeAsync("5\r\nhello\r\n0\r\nX-Custom: value\r\nX-Other: 2\r\n\r\n");

        var body = await HypertextTransferProtocolChunkedBodyReader.ReadAsync(pipe.Reader, CancellationToken.None);

        await Assert.That(body).IsNotNull();
        await Assert.That(Encoding.ASCII.GetString(body!)).IsEqualTo("hello");
    }

    /// <summary>
    ///     Hexadecimal chunk sizes with mixed case are decoded correctly.
    /// </summary>
    [Test]
    public async Task ReadAsync_HexChunkSize_DecodesCorrectly()
    {
        var data = new string('x', 0x1A);
        var pipe = await WriteToPipeAsync($"1a\r\n{data}\r\n0\r\n\r\n");

        var body = await HypertextTransferProtocolChunkedBodyReader.ReadAsync(pipe.Reader, CancellationToken.None);

        await Assert.That(body).IsNotNull();
        await Assert.That(body!.Length).IsEqualTo(0x1A);
    }

    /// <summary>
    ///     A non-hexadecimal chunk size line returns null (malformed input).
    /// </summary>
    [Test]
    public async Task ReadAsync_NonHexChunkSize_ReturnsNull()
    {
        var pipe = await WriteToPipeAsync("zzz\r\nhello\r\n0\r\n\r\n");

        var body = await HypertextTransferProtocolChunkedBodyReader.ReadAsync(pipe.Reader, CancellationToken.None);

        await Assert.That(body).IsNull();
    }

    /// <summary>
    ///     An empty chunk size line returns null.
    /// </summary>
    [Test]
    public async Task ReadAsync_EmptyChunkSizeLine_ReturnsNull()
    {
        var pipe = await WriteToPipeAsync("\r\n");

        var body = await HypertextTransferProtocolChunkedBodyReader.ReadAsync(pipe.Reader, CancellationToken.None);

        await Assert.That(body).IsNull();
    }

    /// <summary>
    ///     Premature EOF (no terminating zero chunk) returns null.
    /// </summary>
    [Test]
    public async Task ReadAsync_PrematureEof_ReturnsNull()
    {
        var pipe = await WriteToPipeAsync("5\r\nhel");

        var body = await HypertextTransferProtocolChunkedBodyReader.ReadAsync(pipe.Reader, CancellationToken.None);

        await Assert.That(body).IsNull();
    }

    /// <summary>
    ///     A chunk whose trailing CRLF is missing returns null.
    /// </summary>
    [Test]
    public async Task ReadAsync_MissingChunkTerminator_ReturnsNull()
    {
        var pipe = await WriteToPipeAsync("5\r\nhelloXX0\r\n\r\n");

        var body = await HypertextTransferProtocolChunkedBodyReader.ReadAsync(pipe.Reader, CancellationToken.None);

        await Assert.That(body).IsNull();
    }

    /// <summary>
    ///     A trailers block that never terminates (EOF before blank line) returns null.
    /// </summary>
    [Test]
    public async Task ReadAsync_TrailersUnterminated_ReturnsNull()
    {
        var pipe = await WriteToPipeAsync("0\r\nX-Custom: value\r\n");

        var body = await HypertextTransferProtocolChunkedBodyReader.ReadAsync(pipe.Reader, CancellationToken.None);

        await Assert.That(body).IsNull();
    }

    /// <summary>
    ///     A chunk size exceeding the safety cap returns null.
    /// </summary>
    [Test]
    public async Task ReadAsync_ChunkSizeExceedsCap_ReturnsNull()
    {
        var pipe = await WriteToPipeAsync("80000000\r\n");

        var body = await HypertextTransferProtocolChunkedBodyReader.ReadAsync(pipe.Reader, CancellationToken.None);

        await Assert.That(body).IsNull();
    }

    private static async Task<Pipe> WriteToPipeAsync(string content)
    {
        var pipe = new Pipe();
        var bytes = Encoding.ASCII.GetBytes(content);
        await pipe.Writer.WriteAsync(bytes);
        await pipe.Writer.CompleteAsync();
        return pipe;
    }
}
