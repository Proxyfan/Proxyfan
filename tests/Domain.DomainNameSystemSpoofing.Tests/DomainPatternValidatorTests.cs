using System.Threading.Tasks;

namespace Proxyfan.Domain.DomainNameSystemSpoofing.Tests;

/// <summary>
///     Tests for <see cref="DomainPatternValidator" />.
/// </summary>
public sealed class DomainPatternValidatorTests
{
    /// <summary>
    ///     Verifies valid patterns are accepted.
    /// </summary>
    /// <param name="pattern">The pattern to validate.</param>
    [Test]
    [Arguments("example.com")]
    [Arguments("api.example.com")]
    [Arguments("deep.api.example.com")]
    [Arguments("EXAMPLE.com")]
    [Arguments("123.numeric-host.io")]
    [Arguments("*.example.com")]
    [Arguments("*.deep.example.com")]
    [Arguments("a.b")]
    [Arguments("single")]
    public async Task HasValidPattern_ValidPattern_ReturnsTrue(string pattern)
    {
        await Assert.That(DomainPatternValidator.HasValidPattern(pattern)).IsTrue();
    }

    /// <summary>
    ///     Verifies invalid patterns are rejected.
    /// </summary>
    /// <param name="pattern">The pattern to validate.</param>
    [Test]
    [Arguments(null)]
    [Arguments("")]
    [Arguments("   ")]
    [Arguments(".example.com")]
    [Arguments("example..com")]
    [Arguments("-example.com")]
    [Arguments("example-.com")]
    [Arguments("ex ample.com")]
    [Arguments("*.")]
    [Arguments("**.example.com")]
    [Arguments("*example.com")]
    [Arguments("foo.*.example.com")]
    [Arguments("http://example.com")]
    [Arguments("example.com/path")]
    [Arguments("example.com:8080")]
    public async Task HasValidPattern_InvalidPattern_ReturnsFalse(string? pattern)
    {
        await Assert.That(DomainPatternValidator.HasValidPattern(pattern)).IsFalse();
    }

    /// <summary>
    ///     Verifies that surrounding whitespace is tolerated and the inner pattern is
    ///     validated.
    /// </summary>
    [Test]
    public async Task HasValidPattern_SurroundingWhitespace_AcceptsTrimmedForm()
    {
        await Assert.That(DomainPatternValidator.HasValidPattern("  api.example.com  ")).IsTrue();
    }
}
