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

    [Test]
    public async Task Render_ApplicationProtobuf_PrettyPrintsAsFieldTree()
    {
        var body = new byte[] { 0x08, 0x96, 0x01 };
        var headers = HeaderCollection.Empty.Add("Content-Type", "application/protobuf");

        var result = InspectorBodyRenderer.Render(body, headers);

        await Assert.That(result).IsEqualTo("Field 1 (varint): 150");
    }

    [Test]
    public async Task Render_ApplicationXProtobuf_PrettyPrintsAsFieldTree()
    {
        var stringBytes = Encoding.UTF8.GetBytes("testing");
        var body = new byte[2 + stringBytes.Length];
        body[0] = 0x12;
        body[1] = (byte)stringBytes.Length;
        stringBytes.CopyTo(body, 2);
        var headers = HeaderCollection.Empty.Add("Content-Type", "application/x-protobuf");

        var result = InspectorBodyRenderer.Render(body, headers);

        await Assert.That(result).IsEqualTo("Field 2 (string): \"testing\"");
    }

    [Test]
    public async Task Render_VendorProtobufSuffix_PrettyPrintsAsFieldTree()
    {
        var body = new byte[] { 0x08, 0x2A };
        var headers = HeaderCollection.Empty.Add("Content-Type", "application/vnd.acme+protobuf");

        var result = InspectorBodyRenderer.Render(body, headers);

        await Assert.That(result).IsEqualTo("Field 1 (varint): 42");
    }

    [Test]
    public async Task Render_FormUrlEncoded_RendersAsKeyValueList()
    {
        var body = Encoding.UTF8.GetBytes("name=alice&age=30");
        var headers = HeaderCollection.Empty.Add("Content-Type", "application/x-www-form-urlencoded");

        var result = InspectorBodyRenderer.Render(body, headers);

        await Assert.That(result).IsEqualTo("name: alice\nage: 30");
    }

    [Test]
    public async Task Render_TextHtml_PrettyPrintsAsXml()
    {
        var body = Encoding.UTF8.GetBytes("<html><body><p>hi</p></body></html>");
        var headers = HeaderCollection.Empty.Add("Content-Type", "text/html");

        var result = InspectorBodyRenderer.Render(body, headers);

        await Assert.That(result.Contains("<html>")).IsTrue();
        await Assert.That(result.Contains('\n')).IsTrue();
    }

    [Test]
    public async Task Render_ImageContent_PrefixesWithMetadataAndHexDump()
    {
        var body = new byte[] { 0x89, 0x50, 0x4E, 0x47 };
        var headers = HeaderCollection.Empty.Add("Content-Type", "image/png");

        var result = InspectorBodyRenderer.Render(body, headers);

        await Assert.That(result.StartsWith("[Image: image/png, 4 bytes]", StringComparison.Ordinal)).IsTrue();
        await Assert.That(result.Contains("89 50 4e 47")).IsTrue();
    }

    [Test]
    public async Task Render_OctetStream_RendersAsBinaryHexDump()
    {
        var body = new byte[] { 0xDE, 0xAD, 0xBE, 0xEF };
        var headers = HeaderCollection.Empty.Add("Content-Type", "application/octet-stream");

        var result = InspectorBodyRenderer.Render(body, headers);

        await Assert.That(result.StartsWith("[Binary: application/octet-stream, 4 bytes]", StringComparison.Ordinal)).IsTrue();
        await Assert.That(result.Contains("de ad be ef")).IsTrue();
    }

    [Test]
    public async Task Render_AudioContent_RendersAsBinaryHexDump()
    {
        var body = new byte[] { 0x49, 0x44, 0x33 };
        var headers = HeaderCollection.Empty.Add("Content-Type", "audio/mpeg");

        var result = InspectorBodyRenderer.Render(body, headers);

        await Assert.That(result.StartsWith("[Binary: audio/mpeg, 3 bytes]", StringComparison.Ordinal)).IsTrue();
    }

    [Test]
    public async Task Render_VideoContent_RendersAsBinaryHexDump()
    {
        var body = new byte[] { 0x00, 0x00, 0x00, 0x20 };
        var headers = HeaderCollection.Empty.Add("Content-Type", "video/mp4");

        var result = InspectorBodyRenderer.Render(body, headers);

        await Assert.That(result.StartsWith("[Binary: video/mp4, 4 bytes]", StringComparison.Ordinal)).IsTrue();
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
