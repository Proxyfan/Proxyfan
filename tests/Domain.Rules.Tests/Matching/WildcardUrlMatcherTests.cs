using Proxyfan.Domain.Rules.Matching;
using System;
using System.Threading.Tasks;

namespace Proxyfan.Domain.Rules.Tests.Matching;

/// <summary>
///     Tests for <see cref="WildcardUrlMatcher" />.
/// </summary>
public sealed class WildcardUrlMatcherTests
{
    /// <summary>
    ///     Verifies that the constructor rejects a null pattern.
    /// </summary>
    [Test]
    public async Task Constructor_WithNullPattern_Throws()
    {
        await Assert.That(() => _ = new WildcardUrlMatcher(null!)).Throws<ArgumentException>();
    }

    /// <summary>
    ///     Verifies that wildcard <c>*</c> matches any sequence of characters.
    /// </summary>
    [Test]
    public async Task Matches_WildcardAny_MatchesAnyUrl()
    {
        var matcher = new WildcardUrlMatcher("*");

        var matchesHttps = matcher.HasMatch("https://anything.example/");
        var matchesEmpty = matcher.HasMatch("a");

        await Assert.That(matchesHttps).IsTrue();
        await Assert.That(matchesEmpty).IsTrue();
    }

    /// <summary>
    ///     Verifies that wildcard <c>*</c> matches subdomain prefixes.
    /// </summary>
    [Test]
    public async Task Matches_SubdomainPattern_MatchesNestedSubdomain()
    {
        var matcher = new WildcardUrlMatcher("https://*.example.com/*");

        var result = matcher.HasMatch("https://api.example.com/users");

        await Assert.That(result).IsTrue();
    }

    /// <summary>
    ///     Verifies that <c>?</c> matches exactly one character.
    /// </summary>
    [Test]
    public async Task Matches_QuestionMark_MatchesSingleCharacter()
    {
        var matcher = new WildcardUrlMatcher("https://a?c.com/");

        var matchesAbc = matcher.HasMatch("https://abc.com/");
        var matchesAbcdc = matcher.HasMatch("https://abdc.com/");

        await Assert.That(matchesAbc).IsTrue();
        await Assert.That(matchesAbcdc).IsFalse();
    }

    /// <summary>
    ///     Verifies that special regex characters in the pattern are escaped.
    /// </summary>
    [Test]
    public async Task Matches_SpecialCharacters_EscapedCorrectly()
    {
        var matcher = new WildcardUrlMatcher("https://example.com/v1.0/items");

        var matches = matcher.HasMatch("https://example.com/v1.0/items");
        var doesNotMatch = matcher.HasMatch("https://example.com/v1x0/items");

        await Assert.That(matches).IsTrue();
        await Assert.That(doesNotMatch).IsFalse();
    }

    /// <summary>
    ///     Verifies that matching is case-insensitive.
    /// </summary>
    [Test]
    public async Task Matches_DifferentCase_StillMatches()
    {
        var matcher = new WildcardUrlMatcher("https://EXAMPLE.com/*");

        var result = matcher.HasMatch("https://example.com/path");

        await Assert.That(result).IsTrue();
    }

    /// <summary>
    ///     Verifies that empty URLs do not match.
    /// </summary>
    [Test]
    public async Task Matches_EmptyUrl_ReturnsFalse()
    {
        var matcher = new WildcardUrlMatcher("*");

        var result = matcher.HasMatch(string.Empty);

        await Assert.That(result).IsFalse();
    }
}
