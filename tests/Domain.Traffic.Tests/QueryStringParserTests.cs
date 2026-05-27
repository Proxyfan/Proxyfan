using System;
using System.Threading.Tasks;
using Proxyfan.Domain.Traffic;

namespace Proxyfan.Domain.Traffic.Tests;

/// <summary>
///     Tests for <see cref="QueryStringParser" />.
/// </summary>
public sealed class QueryStringParserTests
{
    /// <summary>
    ///     Verifies that a null URI returns an empty list.
    /// </summary>
    [Test]
    public async Task Parse_NullUri_ReturnsEmpty()
    {
        var result = QueryStringParser.Parse(null!);

        await Assert.That(result.Count).IsEqualTo(0);
    }

    /// <summary>
    ///     Verifies that an absolute URI without a query returns an empty list.
    /// </summary>
    [Test]
    public async Task Parse_AbsoluteUriWithoutQuery_ReturnsEmpty()
    {
        var uri = new Uri("https://example.com/path");

        var result = QueryStringParser.Parse(uri);

        await Assert.That(result.Count).IsEqualTo(0);
    }

    /// <summary>
    ///     Verifies that a relative URI's query string is honored.
    /// </summary>
    [Test]
    public async Task Parse_RelativeUriWithQuery_ParsesParameters()
    {
        var uri = new Uri("/path?foo=bar", UriKind.Relative);

        var result = QueryStringParser.Parse(uri);

        await Assert.That(result.Count).IsEqualTo(1);
        await Assert.That(result[0].Name).IsEqualTo("foo");
        await Assert.That(result[0].Value).IsEqualTo("bar");
    }

    /// <summary>
    ///     Verifies that a relative URI without a query returns an empty list.
    /// </summary>
    [Test]
    public async Task Parse_RelativeUriWithoutQuery_ReturnsEmpty()
    {
        var uri = new Uri("/path", UriKind.Relative);

        var result = QueryStringParser.Parse(uri);

        await Assert.That(result.Count).IsEqualTo(0);
    }

    /// <summary>
    ///     Verifies that multiple parameters preserve order.
    /// </summary>
    [Test]
    public async Task Parse_MultipleParameters_PreservesOrder()
    {
        var uri = new Uri("https://example.com/?a=1&b=2&c=3");

        var result = QueryStringParser.Parse(uri);

        await Assert.That(result.Count).IsEqualTo(3);
        await Assert.That(result[0].Name).IsEqualTo("a");
        await Assert.That(result[1].Name).IsEqualTo("b");
        await Assert.That(result[2].Name).IsEqualTo("c");
    }

    /// <summary>
    ///     Verifies that a parameter without an equals sign yields an empty value.
    /// </summary>
    [Test]
    public async Task Parse_ParameterWithoutEquals_HasEmptyValue()
    {
        var uri = new Uri("https://example.com/?flag");

        var result = QueryStringParser.Parse(uri);

        await Assert.That(result.Count).IsEqualTo(1);
        await Assert.That(result[0].Name).IsEqualTo("flag");
        await Assert.That(result[0].Value).IsEqualTo(string.Empty);
    }

    /// <summary>
    ///     Verifies that percent-encoded characters and <c>+</c> are decoded.
    /// </summary>
    [Test]
    public async Task Parse_PercentAndPlusEncodedValue_DecodesBoth()
    {
        var uri = new Uri("https://example.com/?q=hello+world%21");

        var result = QueryStringParser.Parse(uri);

        await Assert.That(result.Count).IsEqualTo(1);
        await Assert.That(result[0].Value).IsEqualTo("hello world!");
    }

    /// <summary>
    ///     Verifies that empty segments between ampersands are skipped.
    /// </summary>
    [Test]
    public async Task ParseQueryString_EmptySegments_AreSkipped()
    {
        var result = QueryStringParser.ParseQueryString("?a=1&&b=2");

        await Assert.That(result.Count).IsEqualTo(2);
    }

    /// <summary>
    ///     Verifies that a null or empty raw query string yields an empty list.
    /// </summary>
    /// <param name="raw">The raw query string.</param>
    [Test]
    [Arguments("")]
    [Arguments("?")]
    public async Task ParseQueryString_BlankInput_ReturnsEmpty(string raw)
    {
        var result = QueryStringParser.ParseQueryString(raw);

        await Assert.That(result.Count).IsEqualTo(0);
    }

    /// <summary>
    ///     Verifies that a null raw query string yields an empty list.
    /// </summary>
    [Test]
    public async Task ParseQueryString_NullInput_ReturnsEmpty()
    {
        var result = QueryStringParser.ParseQueryString(null);

        await Assert.That(result.Count).IsEqualTo(0);
    }
}
