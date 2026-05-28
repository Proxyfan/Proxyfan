using Proxyfan.Client.Tools.ViewModels;
using Proxyfan.Domain.Traffic;
using System;
using System.Text;
using System.Threading.Tasks;
using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.Core;

namespace Proxyfan.Client.Tests;

/// <summary>
///     Tests for <see cref="BreakpointMessageTextHelpers" />.
/// </summary>
public sealed class BreakpointMessageTextHelpersTests
{
    [Test]
    public async Task DecodeBody_EmptyMemory_ReturnsEmptyString()
    {
        var result = BreakpointMessageTextHelpers.DecodeBody(ReadOnlyMemory<byte>.Empty);

        await Assert.That(result).IsEqualTo(string.Empty);
    }

    [Test]
    public async Task DecodeBody_Utf8Bytes_ReturnsDecodedString()
    {
        var bytes = Encoding.UTF8.GetBytes("hello world");

        var result = BreakpointMessageTextHelpers.DecodeBody(bytes);

        await Assert.That(result).IsEqualTo("hello world");
    }

    [Test]
    public async Task EncodeBody_NullText_ReturnsEmptyArray()
    {
        var result = BreakpointMessageTextHelpers.EncodeBody(null!);

        await Assert.That(result.Length).IsEqualTo(0);
    }

    [Test]
    public async Task EncodeBody_EmptyText_ReturnsEmptyArray()
    {
        var result = BreakpointMessageTextHelpers.EncodeBody(string.Empty);

        await Assert.That(result.Length).IsEqualTo(0);
    }

    [Test]
    public async Task EncodeBody_NonEmptyText_ReturnsUtf8Bytes()
    {
        var result = BreakpointMessageTextHelpers.EncodeBody("Hi");

        await Assert.That(result.Length).IsEqualTo(2);
        await Assert.That(result[0]).IsEqualTo((byte)'H');
        await Assert.That(result[1]).IsEqualTo((byte)'i');
    }

    [Test]
    public async Task FormatHeaders_EmptyCollection_ReturnsEmptyString()
    {
        var result = BreakpointMessageTextHelpers.FormatHeaders(HeaderCollection.Empty);

        await Assert.That(result).IsEqualTo(string.Empty);
    }

    [Test]
    public async Task FormatHeaders_SingleAndMultiValueHeaders_RendersOnePerLine()
    {
        var headers = HeaderCollection.Empty
            .Add("Host", "example.com")
            .Add("Set-Cookie", "a=1")
            .Add("Set-Cookie", "b=2");

        var result = BreakpointMessageTextHelpers.FormatHeaders(headers);

        await Assert.That(result).IsEqualTo("Host: example.com\nSet-Cookie: a=1\nSet-Cookie: b=2\n");
    }

    [Test]
    public async Task ParseHeaders_NullOrWhitespace_ReturnsEmptyCollection()
    {
        var empty = BreakpointMessageTextHelpers.ParseHeaders(string.Empty);
        var whitespace = BreakpointMessageTextHelpers.ParseHeaders("   \r\n   ");

        await Assert.That(empty.Count).IsEqualTo(0);
        await Assert.That(whitespace.Count).IsEqualTo(0);
    }

    [Test]
    public async Task ParseHeaders_ValidLines_ParsesNamesAndValues()
    {
        var text = "Host: example.com\nContent-Length: 0\r\n";

        var result = BreakpointMessageTextHelpers.ParseHeaders(text);

        await Assert.That(result.Get("Host")).IsEqualTo("example.com");
        await Assert.That(result.Get("Content-Length")).IsEqualTo("0");
    }

    [Test]
    public async Task ParseHeaders_LinesWithoutColon_AreIgnored()
    {
        var text = "no-colon-here\nHost: example.com\nstill-no-colon";

        var result = BreakpointMessageTextHelpers.ParseHeaders(text);

        await Assert.That(result.Get("Host")).IsEqualTo("example.com");
        await Assert.That(result.Count).IsEqualTo(1);
    }

    [Test]
    public async Task ParseHeaders_LeadingColon_IsIgnored()
    {
        var text = ":empty-name: value\nHost: example.com";

        var result = BreakpointMessageTextHelpers.ParseHeaders(text);

        await Assert.That(result.Count).IsEqualTo(1);
        await Assert.That(result.Get("Host")).IsEqualTo("example.com");
    }

    [Test]
    public async Task ParseHeaders_BlankLines_AreSkipped()
    {
        var text = "\n\r\nHost: example.com\n\n";

        var result = BreakpointMessageTextHelpers.ParseHeaders(text);

        await Assert.That(result.Count).IsEqualTo(1);
        await Assert.That(result.Get("Host")).IsEqualTo("example.com");
    }
}
