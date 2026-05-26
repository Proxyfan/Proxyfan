using Proxyfan.Domain.Traffic;
using System;
using System.IO.Pipelines;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Proxyfan.Framework.Networking.Tests;

/// <summary>
///     Tests for <see cref="HypertextTransferProtocolPipeHelpers" />.
/// </summary>
public sealed class HypertextTransferProtocolPipeHelpersTests
{
    /// <summary>
    ///     Verifies that <see cref="HypertextTransferProtocolPipeHelpers.ReadRequestAsync" /> returns
    ///     null when the reader stream is empty.
    /// </summary>
    [Test]
    public async Task ReadRequestAsync_EmptyInput_ReturnsNull()
    {
        var pipe = new Pipe();
        await pipe.Writer.CompleteAsync();

        var result = await HypertextTransferProtocolPipeHelpers.ReadRequestAsync(pipe.Reader, 4096, CancellationToken.None);

        await Assert.That(result).IsNull();
    }

    /// <summary>
    ///     Verifies that <see cref="HypertextTransferProtocolPipeHelpers.ReadRequestAsync" /> returns
    ///     null when the header line cannot be parsed.
    /// </summary>
    [Test]
    public async Task ReadRequestAsync_UnparseableHeaders_ReturnsNull()
    {
        var pipe = new Pipe();
        var bytes = Encoding.ASCII.GetBytes("NOT A VALID REQUEST LINE\r\n\r\n");
        await pipe.Writer.WriteAsync(bytes);
        await pipe.Writer.CompleteAsync();

        var result = await HypertextTransferProtocolPipeHelpers.ReadRequestAsync(pipe.Reader, 4096, CancellationToken.None);

        await Assert.That(result).IsNull();
    }

    /// <summary>
    ///     Verifies that <see cref="HypertextTransferProtocolPipeHelpers.ReadRequestAsync" /> parses
    ///     a valid GET request with no body.
    /// </summary>
    [Test]
    public async Task ReadRequestAsync_ValidGet_ReturnsExchange()
    {
        var pipe = new Pipe();
        var bytes = Encoding.ASCII.GetBytes("GET http://example.com/ HTTP/1.1\r\nHost: example.com\r\n\r\n");
        await pipe.Writer.WriteAsync(bytes);
        await pipe.Writer.CompleteAsync();

        var result = await HypertextTransferProtocolPipeHelpers.ReadRequestAsync(pipe.Reader, 4096, CancellationToken.None);

        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Request.Method).IsEqualTo("GET");
        await Assert.That(result.Body.Length).IsEqualTo(0);
    }

    /// <summary>
    ///     Verifies that <see cref="HypertextTransferProtocolPipeHelpers.ReadRequestAsync" /> reads
    ///     the body when a Content-Length header is present.
    /// </summary>
    [Test]
    public async Task ReadRequestAsync_PostWithBody_ReadsBody()
    {
        var pipe = new Pipe();
        var bytes = Encoding.ASCII.GetBytes("POST http://example.com/ HTTP/1.1\r\nHost: example.com\r\nContent-Length: 5\r\n\r\nhello");
        await pipe.Writer.WriteAsync(bytes);
        await pipe.Writer.CompleteAsync();

        var result = await HypertextTransferProtocolPipeHelpers.ReadRequestAsync(pipe.Reader, 4096, CancellationToken.None);

        await Assert.That(result).IsNotNull();
        await Assert.That(Encoding.ASCII.GetString(result!.Body.Span)).IsEqualTo("hello");
    }

    /// <summary>
    ///     Verifies that <see cref="HypertextTransferProtocolPipeHelpers.ReadRequestAsync" /> returns
    ///     null when the declared body cannot be read in full.
    /// </summary>
    [Test]
    public async Task ReadRequestAsync_TruncatedBody_ReturnsNull()
    {
        var pipe = new Pipe();
        var bytes = Encoding.ASCII.GetBytes("POST http://example.com/ HTTP/1.1\r\nHost: example.com\r\nContent-Length: 100\r\n\r\nshort");
        await pipe.Writer.WriteAsync(bytes);
        await pipe.Writer.CompleteAsync();

        var result = await HypertextTransferProtocolPipeHelpers.ReadRequestAsync(pipe.Reader, 4096, CancellationToken.None);

        await Assert.That(result).IsNull();
    }

    /// <summary>
    ///     Verifies that <see cref="HypertextTransferProtocolPipeHelpers.ReadResponseAsync" /> returns
    ///     null when the reader stream is empty.
    /// </summary>
    [Test]
    public async Task ReadResponseAsync_EmptyInput_ReturnsNull()
    {
        var pipe = new Pipe();
        await pipe.Writer.CompleteAsync();

        var result = await HypertextTransferProtocolPipeHelpers.ReadResponseAsync(pipe.Reader, 4096, CancellationToken.None);

        await Assert.That(result).IsNull();
    }

    /// <summary>
    ///     Verifies that <see cref="HypertextTransferProtocolPipeHelpers.ReadResponseAsync" /> returns
    ///     null when the response status line cannot be parsed.
    /// </summary>
    [Test]
    public async Task ReadResponseAsync_UnparseableHeaders_ReturnsNull()
    {
        var pipe = new Pipe();
        var bytes = Encoding.ASCII.GetBytes("NOT A VALID STATUS LINE\r\n\r\n");
        await pipe.Writer.WriteAsync(bytes);
        await pipe.Writer.CompleteAsync();

        var result = await HypertextTransferProtocolPipeHelpers.ReadResponseAsync(pipe.Reader, 4096, CancellationToken.None);

        await Assert.That(result).IsNull();
    }

    /// <summary>
    ///     Verifies that <see cref="HypertextTransferProtocolPipeHelpers.ReadResponseAsync" /> parses
    ///     a valid response with explicit Content-Length.
    /// </summary>
    [Test]
    public async Task ReadResponseAsync_WithContentLength_ReturnsExchange()
    {
        var pipe = new Pipe();
        var bytes = Encoding.ASCII.GetBytes("HTTP/1.1 200 OK\r\nContent-Length: 5\r\n\r\nhello");
        await pipe.Writer.WriteAsync(bytes);
        await pipe.Writer.CompleteAsync();

        var result = await HypertextTransferProtocolPipeHelpers.ReadResponseAsync(pipe.Reader, 4096, CancellationToken.None);

        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Response.StatusCode).IsEqualTo(200);
        await Assert.That(Encoding.ASCII.GetString(result.Body.Span)).IsEqualTo("hello");
    }

    /// <summary>
    ///     Verifies that <see cref="HypertextTransferProtocolPipeHelpers.ReadResponseAsync" /> reads
    ///     the body until close when no Content-Length is present.
    /// </summary>
    [Test]
    public async Task ReadResponseAsync_WithoutContentLength_ReadsBodyUntilClose()
    {
        var pipe = new Pipe();
        var bytes = Encoding.ASCII.GetBytes("HTTP/1.1 200 OK\r\nServer: test\r\n\r\nhello-body-content");
        await pipe.Writer.WriteAsync(bytes);
        await pipe.Writer.CompleteAsync();

        var result = await HypertextTransferProtocolPipeHelpers.ReadResponseAsync(pipe.Reader, 4096, CancellationToken.None);

        await Assert.That(result).IsNotNull();
        await Assert.That(Encoding.ASCII.GetString(result!.Body.Span)).IsEqualTo("hello-body-content");
    }

    /// <summary>
    ///     Verifies that <see cref="HypertextTransferProtocolPipeHelpers.WriteResponseAsync" /> flushes
    ///     the header and body bytes to the writer.
    /// </summary>
    [Test]
    public async Task WriteResponseAsync_WithExchange_WritesAllBytes()
    {
        var pipe = new Pipe();
        byte[] headerBytes = Encoding.ASCII.GetBytes("HTTP/1.1 200 OK\r\nContent-Length: 5\r\n\r\n");
        byte[] body = Encoding.ASCII.GetBytes("hello");
        var responseParameters = new HypertextTransferProtocolResponseDataParameters
        {
            Body = body,
            Headers = HeaderCollection.Empty.Add("Content-Length", "5"),
            ReasonPhrase = "OK",
            StatusCode = 200,
            Version = "HTTP/1.1",
        };
        var response = new HypertextTransferProtocolResponseData(responseParameters);
        var exchange = new HypertextTransferProtocolProxyResponseExchange(body, headerBytes, response);

        await HypertextTransferProtocolPipeHelpers.WriteResponseAsync(pipe.Writer, exchange, CancellationToken.None);
        await pipe.Writer.CompleteAsync();

        var bytesWritten = await ReadAllAsync(pipe.Reader);
        var text = Encoding.ASCII.GetString(bytesWritten);
        await Assert.That(text).IsEqualTo("HTTP/1.1 200 OK\r\nContent-Length: 5\r\n\r\nhello");
    }

    private static async Task<byte[]> ReadAllAsync(PipeReader reader)
    {
        using var memoryStream = new System.IO.MemoryStream();
        var stream = reader.AsStream();
        await stream.CopyToAsync(memoryStream);
        return memoryStream.ToArray();
    }
}