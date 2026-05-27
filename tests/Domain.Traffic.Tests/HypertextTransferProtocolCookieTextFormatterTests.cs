using System.Threading.Tasks;
using Proxyfan.Domain.Traffic;

namespace Proxyfan.Domain.Traffic.Tests;

/// <summary>
///     Tests for <see cref="HypertextTransferProtocolCookieTextFormatter" />.
/// </summary>
public sealed class HypertextTransferProtocolCookieTextFormatterTests
{
    /// <summary>
    ///     Verifies that an empty list yields an empty string.
    /// </summary>
    [Test]
    public async Task Format_EmptyList_ReturnsEmptyString()
    {
        var result = HypertextTransferProtocolCookieTextFormatter.Format(System.Array.Empty<HypertextTransferProtocolCookie>());

        await Assert.That(result).IsEqualTo(string.Empty);
    }

    /// <summary>
    ///     Verifies that a null list yields an empty string.
    /// </summary>
    [Test]
    public async Task Format_NullList_ReturnsEmptyString()
    {
        var result = HypertextTransferProtocolCookieTextFormatter.Format(null);

        await Assert.That(result).IsEqualTo(string.Empty);
    }

    /// <summary>
    ///     Verifies the header row is present and includes column titles.
    /// </summary>
    [Test]
    public async Task Format_SingleCookie_IncludesHeader()
    {
        var cookie = BuildCookie("session", "abc");
        var result = HypertextTransferProtocolCookieTextFormatter.Format(new[] { cookie });

        await Assert.That(result.Contains("Name", System.StringComparison.Ordinal)).IsTrue();
        await Assert.That(result.Contains("Value", System.StringComparison.Ordinal)).IsTrue();
        await Assert.That(result.Contains("Flags", System.StringComparison.Ordinal)).IsTrue();
    }

    /// <summary>
    ///     Verifies the cookie row appears in the output.
    /// </summary>
    [Test]
    public async Task Format_SingleCookie_ContainsCookieValues()
    {
        var parameters = new HypertextTransferProtocolCookieParameters
        {
            Name = "userid",
            Value = "42",
            Domain = "example.com",
            Path = "/account",
        };
        var cookie = new HypertextTransferProtocolCookie(parameters);

        var result = HypertextTransferProtocolCookieTextFormatter.Format(new[] { cookie });

        await Assert.That(result.Contains("userid", System.StringComparison.Ordinal)).IsTrue();
        await Assert.That(result.Contains("42", System.StringComparison.Ordinal)).IsTrue();
        await Assert.That(result.Contains("example.com", System.StringComparison.Ordinal)).IsTrue();
        await Assert.That(result.Contains("/account", System.StringComparison.Ordinal)).IsTrue();
    }

    /// <summary>
    ///     Verifies Secure flag is rendered.
    /// </summary>
    [Test]
    public async Task Format_SecureCookie_RendersSecureFlag()
    {
        var parameters = new HypertextTransferProtocolCookieParameters
        {
            Name = "sid",
            Value = "x",
            IsSecure = true,
        };
        var cookie = new HypertextTransferProtocolCookie(parameters);

        var result = HypertextTransferProtocolCookieTextFormatter.Format(new[] { cookie });

        await Assert.That(result.Contains("Secure", System.StringComparison.Ordinal)).IsTrue();
    }

    /// <summary>
    ///     Verifies HttpOnly flag is rendered.
    /// </summary>
    [Test]
    public async Task Format_HypertextTransferProtocolOnlyCookie_RendersFlag()
    {
        var parameters = new HypertextTransferProtocolCookieParameters
        {
            Name = "sid",
            Value = "x",
            IsHypertextTransferProtocolOnly = true,
        };
        var cookie = new HypertextTransferProtocolCookie(parameters);

        var result = HypertextTransferProtocolCookieTextFormatter.Format(new[] { cookie });

        await Assert.That(result.Contains("HttpOnly", System.StringComparison.Ordinal)).IsTrue();
    }

    /// <summary>
    ///     Verifies SameSite and Expires are rendered when present.
    /// </summary>
    [Test]
    public async Task Format_SameSiteCookie_RendersAttribute()
    {
        var parameters = new HypertextTransferProtocolCookieParameters
        {
            Name = "csrf",
            Value = "token",
            SameSite = "Strict",
            Expires = "Wed, 09 Jun 2027 10:18:14 GMT",
        };
        var cookie = new HypertextTransferProtocolCookie(parameters);

        var result = HypertextTransferProtocolCookieTextFormatter.Format(new[] { cookie });

        await Assert.That(result.Contains("SameSite=Strict", System.StringComparison.Ordinal)).IsTrue();
        await Assert.That(result.Contains("Expires=Wed, 09 Jun 2027", System.StringComparison.Ordinal)).IsTrue();
    }

    /// <summary>
    ///     Verifies multiple cookies all appear in the output.
    /// </summary>
    [Test]
    public async Task Format_MultipleCookies_RendersAllRows()
    {
        var first = BuildCookie("first", "1");
        var second = BuildCookie("second", "2");

        var result = HypertextTransferProtocolCookieTextFormatter.Format(new[] { first, second });

        await Assert.That(result.Contains("first", System.StringComparison.Ordinal)).IsTrue();
        await Assert.That(result.Contains("second", System.StringComparison.Ordinal)).IsTrue();
    }

    private static HypertextTransferProtocolCookie BuildCookie(string name, string value)
    {
        var parameters = new HypertextTransferProtocolCookieParameters
        {
            Name = name,
            Value = value,
        };
        return new HypertextTransferProtocolCookie(parameters);
    }
}
