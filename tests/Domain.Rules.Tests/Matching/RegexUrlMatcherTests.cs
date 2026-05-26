using Proxyfan.Domain.Rules.Matching;
using System;
using System.Threading.Tasks;

namespace Proxyfan.Domain.Rules.Tests.Matching;

/// <summary>
///     Tests for <see cref="RegexUrlMatcher" />.
/// </summary>
public sealed class RegexUrlMatcherTests
{
    /// <summary>
    ///     Verifies the constructor rejects a null pattern.
    /// </summary>
    [Test]
    public async Task Constructor_WithNullPattern_Throws()
    {
        await Assert.That(() => _ = new RegexUrlMatcher(null!)).Throws<ArgumentException>();
    }

    /// <summary>
    ///     Verifies that a valid regex matches a fitting URL.
    /// </summary>
    [Test]
    public async Task Matches_ValidPattern_MatchesUrl()
    {
        var matcher = new RegexUrlMatcher(@"https://api\.example\.com/v\d+/items");

        var result = matcher.HasMatch("https://api.example.com/v1/items");

        await Assert.That(result).IsTrue();
    }

    /// <summary>
    ///     Verifies that a non-matching URL returns false.
    /// </summary>
    [Test]
    public async Task Matches_NonMatchingUrl_ReturnsFalse()
    {
        var matcher = new RegexUrlMatcher(@"https://api\.example\.com/v\d+");

        var result = matcher.HasMatch("https://other.com/v1");

        await Assert.That(result).IsFalse();
    }

    /// <summary>
    ///     Verifies that an empty URL is rejected.
    /// </summary>
    [Test]
    public async Task Matches_EmptyUrl_ReturnsFalse()
    {
        var matcher = new RegexUrlMatcher(".*");

        var result = matcher.HasMatch(string.Empty);

        await Assert.That(result).IsFalse();
    }

    /// <summary>
    ///     Verifies that matching is case-insensitive.
    /// </summary>
    [Test]
    public async Task Matches_CaseInsensitive_StillMatches()
    {
        var matcher = new RegexUrlMatcher("https://example.com");

        var result = matcher.HasMatch("HTTPS://EXAMPLE.COM");

        await Assert.That(result).IsTrue();
    }

    /// <summary>
    ///     Verifies that an invalid regex syntax throws at construction.
    /// </summary>
    [Test]
    public async Task Constructor_WithInvalidRegex_Throws()
    {
        await Assert.That(() => _ = new RegexUrlMatcher("([unclosed"))
            .Throws<System.Text.RegularExpressions.RegexParseException>();
    }
}
