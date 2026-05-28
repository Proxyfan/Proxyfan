using Proxyfan.Client.Inspector;
using Proxyfan.Domain.Traffic;
using System;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Threading.Tasks;
using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.Core;

namespace Proxyfan.Client.Tests;

/// <summary>
///     Tests for <see cref="InspectorBodyRenderer" />.
/// </summary>
public sealed class InspectorBodyRendererTests
{
    [Test]
    public async Task Render_EmptyBody_ReturnsEmptyString()
    {
        var result = InspectorBodyRenderer.Render(ReadOnlyMemory<byte>.Empty, HeaderCollection.Empty);

        await Assert.That(result).IsEqualTo(string.Empty);
    }

    [Test]
    public async Task Render_PlainTextNoContentType_ReturnsUtf8DecodedText()
    {
        var body = Encoding.UTF8.GetBytes("hello");

        var result = InspectorBodyRenderer.Render(body, HeaderCollection.Empty);

        await Assert.That(result).IsEqualTo("hello");
    }

    [Test]
    public async Task Render_ApplicationJson_PrettyPrints()
    {
        var body = Encoding.UTF8.GetBytes("{\"a\":1}");
        var headers = HeaderCollection.Empty.Add("Content-Type", "application/json");

        var result = InspectorBodyRenderer.Render(body, headers);

        await Assert.That(result.Contains("\"a\"")).IsTrue();
        await Assert.That(result.Contains('\n')).IsTrue();
    }

    [Test]
    public async Task Render_VendorJsonSuffix_PrettyPrints()
    {
        var body = Encoding.UTF8.GetBytes("{\"x\":2}");
        var headers = HeaderCollection.Empty.Add("Content-Type", "application/vnd.api+json");

        var result = InspectorBodyRenderer.Render(body, headers);

        await Assert.That(result.Contains("\"x\"")).IsTrue();
        await Assert.That(result.Contains('\n')).IsTrue();
    }

    [Test]
    public async Task Render_ApplicationXml_PrettyPrints()
    {
        var body = Encoding.UTF8.GetBytes("<root><a>1</a></root>");
        var headers = HeaderCollection.Empty.Add("Content-Type", "application/xml");

        var result = InspectorBodyRenderer.Render(body, headers);

        await Assert.That(result.Contains("<root>")).IsTrue();
        await Assert.That(result.Contains('\n')).IsTrue();
    }

    [Test]
    public async Task Render_TextXml_PrettyPrints()
    {
        var body = Encoding.UTF8.GetBytes("<root><a>1</a></root>");
        var headers = HeaderCollection.Empty.Add("Content-Type", "text/xml");

        var result = InspectorBodyRenderer.Render(body, headers);

        await Assert.That(result.Contains("<root>")).IsTrue();
    }

    [Test]
    public async Task Render_VendorXmlSuffix_PrettyPrints()
    {
        var body = Encoding.UTF8.GetBytes("<a>1</a>");
        var headers = HeaderCollection.Empty.Add("Content-Type", "application/atom+xml");

        var result = InspectorBodyRenderer.Render(body, headers);

        await Assert.That(result.Contains("<a>")).IsTrue();
    }

    [Test]
    public async Task Render_UnknownMediaType_ReturnsPlainText()
    {
        var body = Encoding.UTF8.GetBytes("plain text body");
        var headers = HeaderCollection.Empty.Add("Content-Type", "text/plain");

        var result = InspectorBodyRenderer.Render(body, headers);

        await Assert.That(result).IsEqualTo("plain text body");
    }

    [Test]
    public async Task Render_ExplicitCharsetWindows1252_DecodesAccordingly()
    {
        var body = new byte[] { 0xE9 };
        var headers = HeaderCollection.Empty.Add("Content-Type", "text/plain; charset=windows-1252");

        var result = InspectorBodyRenderer.Render(body, headers);

        await Assert.That(result).IsEqualTo("é");
    }

    [Test]
    public async Task Render_UnknownCharset_FallsBackToUtf8()
    {
        var body = Encoding.UTF8.GetBytes("data");
        var headers = HeaderCollection.Empty.Add("Content-Type", "text/plain; charset=not-a-real-charset");

        var result = InspectorBodyRenderer.Render(body, headers);

        await Assert.That(result).IsEqualTo("data");
    }

    [Test]
    public async Task Render_GzipEncoded_DecompressesBody()
    {
        var original = Encoding.UTF8.GetBytes("compressed payload");
        var compressed = Gzip(original);
        var headers = HeaderCollection.Empty
            .Add("Content-Type", "text/plain")
            .Add("Content-Encoding", "gzip");

        var result = InspectorBodyRenderer.Render(compressed, headers);

        await Assert.That(result).IsEqualTo("compressed payload");
    }

    [Test]
    public async Task Render_UnsupportedEncoding_FallsBackToRawBytes()
    {
        var body = Encoding.UTF8.GetBytes("raw");
        var headers = HeaderCollection.Empty
            .Add("Content-Type", "text/plain")
            .Add("Content-Encoding", "made-up-encoding");

        var result = InspectorBodyRenderer.Render(body, headers);

        await Assert.That(result).IsEqualTo("raw");
    }

    private static byte[] Gzip(byte[] data)
    {
        using var output = new MemoryStream();
        using (var gzip = new GZipStream(output, CompressionLevel.Optimal, leaveOpen: true))
        {
            gzip.Write(data, 0, data.Length);
        }

        return output.ToArray();
    }
}
