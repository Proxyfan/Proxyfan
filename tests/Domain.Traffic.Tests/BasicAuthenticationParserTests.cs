using System;
using System.Text;
using System.Threading.Tasks;

namespace Proxyfan.Domain.Traffic.Tests;

/// <summary>
///     Tests for <see cref="BasicAuthenticationParser" />.
/// </summary>
public sealed class BasicAuthenticationParserTests
{
    /// <summary>
    ///     Verifies that "Basic <base64(user:pass)>" parses into username/password.
    /// </summary>
    [Test]
    public async Task Parse_StandardHeaderValue_ParsesBoth()
    {
        var encoded = Convert.ToBase64String(Encoding.UTF8.GetBytes("alice:secret"));
        var headerValue = "Basic " + encoded;

        var credentials = BasicAuthenticationParser.Parse(headerValue);

        await Assert.That(credentials).IsNotNull();
        await Assert.That(credentials!.UserName).IsEqualTo("alice");
        await Assert.That(credentials.Password).IsEqualTo("secret");
    }

    /// <summary>
    ///     Verifies that a lowercase "basic" scheme is accepted.
    /// </summary>
    [Test]
    public async Task Parse_LowercaseScheme_StillParses()
    {
        var encoded = Convert.ToBase64String(Encoding.UTF8.GetBytes("user:pass"));

        var credentials = BasicAuthenticationParser.Parse("basic " + encoded);

        await Assert.That(credentials!.UserName).IsEqualTo("user");
    }

    /// <summary>
    ///     Verifies that a bare base64 payload (no "Basic" prefix) parses.
    /// </summary>
    [Test]
    public async Task Parse_BareBase64_StillParses()
    {
        var encoded = Convert.ToBase64String(Encoding.UTF8.GetBytes("user:pw"));

        var credentials = BasicAuthenticationParser.Parse(encoded);

        await Assert.That(credentials!.UserName).IsEqualTo("user");
        await Assert.That(credentials.Password).IsEqualTo("pw");
    }

    /// <summary>
    ///     Verifies that an empty password is supported (e.g. "user:").
    /// </summary>
    [Test]
    public async Task Parse_EmptyPassword_IsSupported()
    {
        var encoded = Convert.ToBase64String(Encoding.UTF8.GetBytes("user:"));

        var credentials = BasicAuthenticationParser.Parse("Basic " + encoded);

        await Assert.That(credentials!.UserName).IsEqualTo("user");
        await Assert.That(credentials.Password).IsEqualTo(string.Empty);
    }

    /// <summary>
    ///     Verifies that a password containing colons keeps the trailing portion intact.
    /// </summary>
    [Test]
    public async Task Parse_PasswordWithColons_KeepsTrailingPortion()
    {
        var encoded = Convert.ToBase64String(Encoding.UTF8.GetBytes("user:p:ass:word"));

        var credentials = BasicAuthenticationParser.Parse("Basic " + encoded);

        await Assert.That(credentials!.Password).IsEqualTo("p:ass:word");
    }

    /// <summary>
    ///     Verifies that null and blank values return null.
    /// </summary>
    /// <param name="value">The header value to test.</param>
    [Test]
    [Arguments(null)]
    [Arguments("")]
    [Arguments("   ")]
    public async Task Parse_NullOrBlankValue_ReturnsNull(string? value)
    {
        var credentials = BasicAuthenticationParser.Parse(value);

        await Assert.That(credentials).IsNull();
    }

    /// <summary>
    ///     Verifies that "Basic " with no payload returns null.
    /// </summary>
    [Test]
    public async Task Parse_SchemeOnly_ReturnsNull()
    {
        var credentials = BasicAuthenticationParser.Parse("Basic ");

        await Assert.That(credentials).IsNull();
    }

    /// <summary>
    ///     Verifies that an invalid base64 payload returns null.
    /// </summary>
    [Test]
    public async Task Parse_InvalidBase64_ReturnsNull()
    {
        var credentials = BasicAuthenticationParser.Parse("Basic not-base64!!!");

        await Assert.That(credentials).IsNull();
    }

    /// <summary>
    ///     Verifies that a decoded payload without a colon returns null.
    /// </summary>
    [Test]
    public async Task Parse_DecodedPayloadWithoutColon_ReturnsNull()
    {
        var encoded = Convert.ToBase64String(Encoding.UTF8.GetBytes("nocolon"));

        var credentials = BasicAuthenticationParser.Parse("Basic " + encoded);

        await Assert.That(credentials).IsNull();
    }
}
