using System.Threading.Tasks;

namespace Proxyfan.Domain.Traffic.Tests;

/// <summary>
///     Tests for <see cref="HypertextTransferProtocolCookieParser" />.
/// </summary>
public sealed class HypertextTransferProtocolCookieParserTests
{
    /// <summary>
    ///     Verifies that an empty Cookie header yields no cookies.
    /// </summary>
    [Test]
    public async Task ParseRequestCookies_EmptyHeader_ReturnsEmpty()
    {
        var cookies = HypertextTransferProtocolCookieParser.ParseRequestCookies(string.Empty);

        await Assert.That(cookies.Count).IsEqualTo(0);
    }

    /// <summary>
    ///     Verifies that a single Cookie pair parses.
    /// </summary>
    [Test]
    public async Task ParseRequestCookies_SinglePair_ParsesNameAndValue()
    {
        var cookies = HypertextTransferProtocolCookieParser.ParseRequestCookies("session=abc123");

        await Assert.That(cookies.Count).IsEqualTo(1);
        await Assert.That(cookies[0].Name).IsEqualTo("session");
        await Assert.That(cookies[0].Value).IsEqualTo("abc123");
    }

    /// <summary>
    ///     Verifies that multiple semicolon-separated Cookie pairs all parse.
    /// </summary>
    [Test]
    public async Task ParseRequestCookies_MultipleCookies_ParsesAll()
    {
        var cookies = HypertextTransferProtocolCookieParser.ParseRequestCookies("a=1; b=2; c=3");

        await Assert.That(cookies.Count).IsEqualTo(3);
        await Assert.That(cookies[1].Name).IsEqualTo("b");
        await Assert.That(cookies[1].Value).IsEqualTo("2");
    }

    /// <summary>
    ///     Verifies that a malformed pair (no equals) is skipped.
    /// </summary>
    [Test]
    public async Task ParseRequestCookies_MalformedPair_IsSkipped()
    {
        var cookies = HypertextTransferProtocolCookieParser.ParseRequestCookies("noequals; valid=value");

        await Assert.That(cookies.Count).IsEqualTo(1);
        await Assert.That(cookies[0].Name).IsEqualTo("valid");
    }

    /// <summary>
    ///     Verifies that a basic Set-Cookie with name=value parses.
    /// </summary>
    [Test]
    public async Task ParseSetCookie_NameValueOnly_Parses()
    {
        var cookie = HypertextTransferProtocolCookieParser.ParseSetCookie("session=abc123");

        await Assert.That(cookie).IsNotNull();
        await Assert.That(cookie!.Name).IsEqualTo("session");
        await Assert.That(cookie.Value).IsEqualTo("abc123");
        await Assert.That(cookie.IsSecure).IsFalse();
        await Assert.That(cookie.IsHypertextTransferProtocolOnly).IsFalse();
    }

    /// <summary>
    ///     Verifies that all Set-Cookie attributes parse.
    /// </summary>
    [Test]
    public async Task ParseSetCookie_AllAttributes_Parses()
    {
        var cookie = HypertextTransferProtocolCookieParser.ParseSetCookie(
            "session=abc; Domain=example.com; Path=/; Expires=Wed, 01 Jan 2030 00:00:00 GMT; Secure; HttpOnly; SameSite=Strict");

        await Assert.That(cookie!.Domain).IsEqualTo("example.com");
        await Assert.That(cookie.Path).IsEqualTo("/");
        await Assert.That(cookie.IsSecure).IsTrue();
        await Assert.That(cookie.IsHypertextTransferProtocolOnly).IsTrue();
        await Assert.That(cookie.SameSite).IsEqualTo("Strict");
    }

    /// <summary>
    ///     Verifies that a malformed Set-Cookie (no equals) returns null.
    /// </summary>
    [Test]
    public async Task ParseSetCookie_MalformedPair_ReturnsNull()
    {
        var cookie = HypertextTransferProtocolCookieParser.ParseSetCookie("just-a-name");

        await Assert.That(cookie).IsNull();
    }

    /// <summary>
    ///     Verifies that an empty Set-Cookie returns null.
    /// </summary>
    [Test]
    public async Task ParseSetCookie_EmptyValue_ReturnsNull()
    {
        var cookie = HypertextTransferProtocolCookieParser.ParseSetCookie(string.Empty);

        await Assert.That(cookie).IsNull();
    }

    /// <summary>
    ///     Verifies that attribute-name matching is case-insensitive.
    /// </summary>
    [Test]
    public async Task ParseSetCookie_LowerCaseAttributes_StillParse()
    {
        var cookie = HypertextTransferProtocolCookieParser.ParseSetCookie(
            "session=abc; domain=example.com; secure; httponly");

        await Assert.That(cookie!.Domain).IsEqualTo("example.com");
        await Assert.That(cookie.IsSecure).IsTrue();
        await Assert.That(cookie.IsHypertextTransferProtocolOnly).IsTrue();
    }
}
