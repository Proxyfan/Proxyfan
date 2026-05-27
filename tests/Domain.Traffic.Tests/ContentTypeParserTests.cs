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
}
