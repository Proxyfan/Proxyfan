using System.Threading.Tasks;

namespace Proxyfan.Domain.Traffic.Tests;

/// <summary>
///     Tests for <see cref="ContentTypeParser" />.
/// </summary>
public sealed class ContentTypeParserTests
{
    /// <summary>
    ///     Verifies that an empty value returns null.
    /// </summary>
    [Test]
    public async Task Parse_EmptyValue_ReturnsNull()
    {
        var parsed = ContentTypeParser.Parse(string.Empty);

        await Assert.That(parsed).IsNull();
    }

    /// <summary>
    ///     A value composed solely of separator characters (e.g. <c>;;;</c>) has no media-type
    ///     and must return null after the split-and-trim pass yields an empty token array.
    /// </summary>
    [Test]
    public async Task Parse_OnlySeparators_ReturnsNull()
    {
        var parsed = ContentTypeParser.Parse(";;;");

        await Assert.That(parsed).IsNull();
    }

    /// <summary>
    ///     Verifies the parser preserves the raw input value on the returned instance for
    ///     downstream consumers that need the original header text.
    /// </summary>
    [Test]
    public async Task Parse_AnyValue_PreservesRawValueOnResult()
    {
        const string raw = "application/json; charset=utf-8";

        var parsed = ContentTypeParser.Parse(raw);

        await Assert.That(parsed!.RawValue).IsEqualTo(raw);
    }

    /// <summary>
    ///     Verifies that a bare media type is parsed without parameters.
    /// </summary>
    [Test]
    public async Task Parse_BareMediaType_ParsesMediaTypeOnly()
    {
        var parsed = ContentTypeParser.Parse("application/json");

        await Assert.That(parsed!.MediaType).IsEqualTo("application/json");
        await Assert.That(parsed.Charset).IsNull();
        await Assert.That(parsed.Boundary).IsNull();
    }

    /// <summary>
    ///     Verifies that a charset parameter is captured.
    /// </summary>
    [Test]
    public async Task Parse_WithCharset_CapturesCharset()
    {
        var parsed = ContentTypeParser.Parse("text/html; charset=utf-8");

        await Assert.That(parsed!.Charset).IsEqualTo("utf-8");
    }

    /// <summary>
    ///     Verifies that a quoted charset value has its quotes stripped.
    /// </summary>
    [Test]
    public async Task Parse_QuotedCharset_StripsQuotes()
    {
        var parsed = ContentTypeParser.Parse("text/html; charset=\"utf-8\"");

        await Assert.That(parsed!.Charset).IsEqualTo("utf-8");
    }

    /// <summary>
    ///     Verifies that a boundary parameter is captured.
    /// </summary>
    [Test]
    public async Task Parse_WithBoundary_CapturesBoundary()
    {
        var parsed = ContentTypeParser.Parse("multipart/form-data; boundary=abc123");

        await Assert.That(parsed!.Boundary).IsEqualTo("abc123");
    }

    /// <summary>
    ///     Verifies that parameter names are matched case-insensitively.
    /// </summary>
    [Test]
    public async Task Parse_UppercaseCharsetName_IsRecognized()
    {
        var parsed = ContentTypeParser.Parse("text/html; CHARSET=utf-8");

        await Assert.That(parsed!.Charset).IsEqualTo("utf-8");
    }

    /// <summary>
    ///     Verifies that a parameter without "=" is silently ignored.
    /// </summary>
    [Test]
    public async Task Parse_MalformedParameter_IsIgnored()
    {
        var parsed = ContentTypeParser.Parse("text/html; novalue");

        await Assert.That(parsed!.MediaType).IsEqualTo("text/html");
        await Assert.That(parsed.Charset).IsNull();
    }

    /// <summary>
    ///     Verifies that a semicolon inside a quoted boundary value does not split the parameter.
    /// </summary>
    [Test]
    public async Task Parse_QuotedBoundaryWithSemicolon_PreservesValue()
    {
        var parsed = ContentTypeParser.Parse("multipart/form-data; boundary=\"abc;def\"");

        await Assert.That(parsed!.MediaType).IsEqualTo("multipart/form-data");
        await Assert.That(parsed.Boundary).IsEqualTo("abc;def");
    }

    /// <summary>
    ///     Verifies that a semicolon inside a quoted charset value does not split the parameter
    ///     and that subsequent parameters are still captured.
    /// </summary>
    [Test]
    public async Task Parse_QuotedCharsetWithSemicolonFollowedByBoundary_CapturesBoth()
    {
        var parsed = ContentTypeParser.Parse("multipart/mixed; charset=\"utf;8\"; boundary=xyz");

        await Assert.That(parsed!.Charset).IsEqualTo("utf;8");
        await Assert.That(parsed.Boundary).IsEqualTo("xyz");
    }

    /// <summary>
    ///     Verifies that a backslash-escaped quote inside a quoted value is unescaped and does
    ///     not terminate the quoted string prematurely.
    /// </summary>
    [Test]
    public async Task Parse_QuotedBoundaryWithEscapedQuote_UnescapesValue()
    {
        var parsed = ContentTypeParser.Parse("multipart/form-data; boundary=\"a\\\"b;c\"");

        await Assert.That(parsed!.Boundary).IsEqualTo("a\"b;c");
    }
}
