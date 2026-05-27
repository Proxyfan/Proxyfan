using System.Threading.Tasks;

namespace Proxyfan.Domain.Traffic.Tests;

/// <summary>
///     Tests for <see cref="StrictTransportSecurityParser" />.
/// </summary>
public sealed class StrictTransportSecurityParserTests
{
    /// <summary>
    ///     Verifies a max-age-only directive parses.
    /// </summary>
    [Test]
    public async Task Parse_MaxAgeOnly_Parses()
    {
        var directive = StrictTransportSecurityParser.Parse("max-age=31536000");

        await Assert.That(directive).IsNotNull();
        await Assert.That(directive!.MaxAgeSeconds).IsEqualTo(31536000L);
        await Assert.That(directive.AllowsSubDomains).IsFalse();
        await Assert.That(directive.IsPreloadable).IsFalse();
    }

    /// <summary>
    ///     Verifies AllowsSubDomains is captured.
    /// </summary>
    [Test]
    public async Task Parse_WithAllowsSubDomains_CapturesFlag()
    {
        var directive = StrictTransportSecurityParser.Parse("max-age=86400; includeSubDomains");

        await Assert.That(directive!.AllowsSubDomains).IsTrue();
    }

    /// <summary>
    ///     Verifies preload is captured.
    /// </summary>
    [Test]
    public async Task Parse_WithPreload_CapturesFlag()
    {
        var directive = StrictTransportSecurityParser.Parse("max-age=86400; preload");

        await Assert.That(directive!.IsPreloadable).IsTrue();
    }

    /// <summary>
    ///     Verifies all three directives together.
    /// </summary>
    [Test]
    public async Task Parse_AllDirectives_CapturesAll()
    {
        var directive = StrictTransportSecurityParser.Parse("max-age=63072000; includeSubDomains; preload");

        await Assert.That(directive!.MaxAgeSeconds).IsEqualTo(63072000L);
        await Assert.That(directive.AllowsSubDomains).IsTrue();
        await Assert.That(directive.IsPreloadable).IsTrue();
    }

    /// <summary>
    ///     Verifies a quoted max-age value is supported.
    /// </summary>
    [Test]
    public async Task Parse_QuotedMaxAge_Parses()
    {
        var directive = StrictTransportSecurityParser.Parse("max-age=\"31536000\"");

        await Assert.That(directive!.MaxAgeSeconds).IsEqualTo(31536000L);
    }

    /// <summary>
    ///     Verifies that a missing max-age returns null.
    /// </summary>
    [Test]
    public async Task Parse_NoMaxAge_ReturnsNull()
    {
        var directive = StrictTransportSecurityParser.Parse("AllowsSubDomains; preload");

        await Assert.That(directive).IsNull();
    }

    /// <summary>
    ///     Verifies that a negative max-age is treated as missing.
    /// </summary>
    [Test]
    public async Task Parse_NegativeMaxAge_ReturnsNull()
    {
        var directive = StrictTransportSecurityParser.Parse("max-age=-1");

        await Assert.That(directive).IsNull();
    }

    /// <summary>
    ///     Verifies that a non-numeric max-age is treated as missing.
    /// </summary>
    [Test]
    public async Task Parse_NonNumericMaxAge_ReturnsNull()
    {
        var directive = StrictTransportSecurityParser.Parse("max-age=abc");

        await Assert.That(directive).IsNull();
    }

    /// <summary>
    ///     Verifies blank input returns null.
    /// </summary>
    [Test]
    [Arguments(null)]
    [Arguments("")]
    [Arguments("   ")]
    public async Task Parse_BlankValue_ReturnsNull(string? value)
    {
        var directive = StrictTransportSecurityParser.Parse(value);

        await Assert.That(directive).IsNull();
    }
}
