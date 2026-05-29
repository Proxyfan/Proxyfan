using System;
using System.Threading.Tasks;

namespace Proxyfan.Domain.DomainNameSystemSpoofing.Tests;

/// <summary>
///     Tests for <see cref="DomainPatternNormalization" />.
/// </summary>
public sealed class DomainPatternNormalizationTests
{
    /// <summary>
    ///     Verifies the canonical form trims whitespace and lower-cases ASCII characters.
    /// </summary>
    [Test]
    public async Task Normalize_MixedCaseWithSurroundingWhitespace_TrimsAndLowercases()
    {
        var result = DomainPatternNormalization.Normalize("  Api.Example.COM  ");

        await Assert.That(result).IsEqualTo("api.example.com");
    }

    /// <summary>
    ///     Verifies a trailing FQDN dot is stripped.
    /// </summary>
    [Test]
    public async Task Normalize_TrailingDot_IsStripped()
    {
        var result = DomainPatternNormalization.Normalize("api.example.com.");

        await Assert.That(result).IsEqualTo("api.example.com");
    }

    /// <summary>
    ///     Verifies a wildcard prefix is preserved.
    /// </summary>
    [Test]
    public async Task Normalize_WildcardPattern_PreservesPrefix()
    {
        var result = DomainPatternNormalization.Normalize("*.Example.COM");

        await Assert.That(result).IsEqualTo("*.example.com");
    }

    /// <summary>
    ///     Verifies a null hostname is rejected.
    /// </summary>
    [Test]
    public async Task Normalize_NullHostname_Throws()
    {
        await Assert.That(() => DomainPatternNormalization.Normalize(null!))
            .Throws<ArgumentException>();
    }

    /// <summary>
    ///     Verifies a whitespace-only hostname is rejected.
    /// </summary>
    [Test]
    public async Task Normalize_WhitespaceHostname_Throws()
    {
        await Assert.That(() => DomainPatternNormalization.Normalize("   "))
            .Throws<ArgumentException>();
    }
}
