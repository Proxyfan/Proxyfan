using System.Threading.Tasks;

namespace Proxyfan.Domain.Traffic.Tests;

/// <summary>
///     Tests for <see cref="CacheControlParser" />.
/// </summary>
public sealed class CacheControlParserTests
{
    /// <summary>
    ///     Verifies that blank input yields default (all-false) directives.
    /// </summary>
    [Test]
    [Arguments(null)]
    [Arguments("")]
    [Arguments("   ")]
    public async Task Parse_BlankInput_ReturnsDefaults(string? value)
    {
        var directives = CacheControlParser.Parse(value);

        await Assert.That(directives.IsNoCache).IsFalse();
        await Assert.That(directives.MaxAgeSeconds).IsNull();
    }

    /// <summary>
    ///     Verifies that each flag is captured.
    /// </summary>
    /// <param name="directive">The directive value.</param>
    [Test]
    [Arguments("no-cache")]
    [Arguments("NO-CACHE")]
    public async Task Parse_NoCacheDirective_SetsFlag(string directive)
    {
        var directives = CacheControlParser.Parse(directive);

        await Assert.That(directives.IsNoCache).IsTrue();
    }

    /// <summary>
    ///     Verifies that no-store, public, private, must-revalidate are captured.
    /// </summary>
    [Test]
    public async Task Parse_AllFlags_AreCaptured()
    {
        var directives = CacheControlParser.Parse("no-store, public, private, must-revalidate");

        await Assert.That(directives.IsNoStore).IsTrue();
        await Assert.That(directives.IsPublic).IsTrue();
        await Assert.That(directives.IsPrivate).IsTrue();
        await Assert.That(directives.IsMustRevalidate).IsTrue();
    }

    /// <summary>
    ///     Verifies that max-age and s-maxage are captured.
    /// </summary>
    [Test]
    public async Task Parse_MaxAgeAndShared_AreCaptured()
    {
        var directives = CacheControlParser.Parse("max-age=3600, s-maxage=1800");

        await Assert.That(directives.MaxAgeSeconds).IsEqualTo(3600L);
        await Assert.That(directives.SharedMaxAgeSeconds).IsEqualTo(1800L);
    }

    /// <summary>
    ///     Verifies that a quoted max-age value is supported.
    /// </summary>
    [Test]
    public async Task Parse_QuotedMaxAge_IsParsed()
    {
        var directives = CacheControlParser.Parse("max-age=\"600\"");

        await Assert.That(directives.MaxAgeSeconds).IsEqualTo(600L);
    }

    /// <summary>
    ///     Verifies that an invalid max-age is silently dropped.
    /// </summary>
    [Test]
    public async Task Parse_InvalidMaxAge_IsDropped()
    {
        var directives = CacheControlParser.Parse("max-age=invalid");

        await Assert.That(directives.MaxAgeSeconds).IsNull();
    }

    /// <summary>
    ///     Verifies that a negative max-age is silently dropped.
    /// </summary>
    [Test]
    public async Task Parse_NegativeMaxAge_IsDropped()
    {
        var directives = CacheControlParser.Parse("max-age=-1");

        await Assert.That(directives.MaxAgeSeconds).IsNull();
    }

    /// <summary>
    ///     Verifies that an unknown directive is silently ignored.
    /// </summary>
    [Test]
    public async Task Parse_UnknownDirective_IsIgnored()
    {
        var directives = CacheControlParser.Parse("no-cache, x-custom=42");

        await Assert.That(directives.IsNoCache).IsTrue();
    }

    /// <summary>
    ///     Verifies that an invalid s-maxage is silently dropped.
    /// </summary>
    [Test]
    public async Task Parse_InvalidSMaxAge_IsDropped()
    {
        var directives = CacheControlParser.Parse("s-maxage=abc");

        await Assert.That(directives.SharedMaxAgeSeconds).IsNull();
    }
}
