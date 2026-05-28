using System.Threading.Tasks;

namespace Proxyfan.Domain.Proxy.Tests;

/// <summary>
///     Tests for <see cref="BypassPatternMatcher" />.
/// </summary>
public sealed class BypassPatternMatcherTests
{
    /// <summary>
    ///     Verifies that an empty pattern list never matches.
    /// </summary>
    [Test]
    public async Task HasMatch_EmptyPatterns_ReturnsFalse()
    {
        var match = BypassPatternMatcher.HasMatch([], "example.com");

        await Assert.That(match).IsFalse();
    }

    /// <summary>
    ///     Verifies that an exact pattern matches the host case-insensitively.
    /// </summary>
    [Test]
    public async Task HasMatch_ExactPatternCaseInsensitive_ReturnsTrue()
    {
        var match = BypassPatternMatcher.HasMatch(["Example.com"], "example.com");

        await Assert.That(match).IsTrue();
    }

    /// <summary>
    ///     Verifies that a non-matching exact pattern returns false.
    /// </summary>
    [Test]
    public async Task HasMatch_ExactPatternMismatch_ReturnsFalse()
    {
        var match = BypassPatternMatcher.HasMatch(["other.example"], "example.com");

        await Assert.That(match).IsFalse();
    }

    /// <summary>
    ///     Verifies that a single <c>*</c> wildcard matches any prefix.
    /// </summary>
    [Test]
    public async Task HasMatch_StarPrefixWildcard_ReturnsTrue()
    {
        var match = BypassPatternMatcher.HasMatch(["*.internal"], "build.internal");

        await Assert.That(match).IsTrue();
    }

    /// <summary>
    ///     Verifies that a <c>*</c> wildcard matches the empty string (zero characters).
    /// </summary>
    [Test]
    public async Task HasMatch_StarMatchesEmpty_ReturnsTrue()
    {
        var match = BypassPatternMatcher.HasMatch(["*internal"], "internal");

        await Assert.That(match).IsTrue();
    }

    /// <summary>
    ///     Verifies that a <c>?</c> wildcard matches exactly one character.
    /// </summary>
    [Test]
    public async Task HasMatch_QuestionMarkWildcard_MatchesSingleCharacter()
    {
        var match = BypassPatternMatcher.HasMatch(["host?.local"], "hostA.local");

        await Assert.That(match).IsTrue();
    }

    /// <summary>
    ///     Verifies that a <c>?</c> wildcard does not match two characters.
    /// </summary>
    [Test]
    public async Task HasMatch_QuestionMarkWildcard_DoesNotMatchTwoCharacters()
    {
        var match = BypassPatternMatcher.HasMatch(["host?.local"], "hostAB.local");

        await Assert.That(match).IsFalse();
    }

    /// <summary>
    ///     Verifies that blank patterns in the list are ignored.
    /// </summary>
    [Test]
    public async Task HasMatch_BlankPatternsAreSkipped_ReturnsTrueOnRealPattern()
    {
        var match = BypassPatternMatcher.HasMatch(["", "   ", "*.local"], "host.local");

        await Assert.That(match).IsTrue();
    }

    /// <summary>
    ///     Verifies that a non-empty list with no matching pattern returns false.
    /// </summary>
    [Test]
    public async Task HasMatch_NoMatchingPattern_ReturnsFalse()
    {
        var match = BypassPatternMatcher.HasMatch(["*.internal", "*.corp"], "external.com");

        await Assert.That(match).IsFalse();
    }

    /// <summary>
    ///     Verifies that multiple wildcards in one pattern are honored.
    /// </summary>
    [Test]
    public async Task HasMatch_MultipleWildcards_ReturnsTrue()
    {
        var match = BypassPatternMatcher.HasMatch(["*.build.*.local"], "ci.build.us.local");

        await Assert.That(match).IsTrue();
    }

    /// <summary>
    ///     A trailing star in the pattern consumes the remaining host characters
    ///     (covers the trailing-* loop true branch).
    /// </summary>
    [Test]
    public async Task HasMatch_PatternEndingWithStar_MatchesLongerHost()
    {
        var match = BypassPatternMatcher.HasMatch(["host*"], "hostnamesuffix");

        await Assert.That(match).IsTrue();
    }

    /// <summary>
    ///     A pattern longer than the host still terminates correctly when no wildcard remains
    ///     (covers the patternIndex &gt;= pattern.Length branch of the main loop).
    /// </summary>
    [Test]
    public async Task HasMatch_PatternLongerThanHost_ReturnsFalse()
    {
        var match = BypassPatternMatcher.HasMatch(["abcde"], "abc");

        await Assert.That(match).IsFalse();
    }

    /// <summary>
    ///     A pattern ending in consecutive stars exercises the trailing-star cleanup loop body
    ///     more than once and must still consider the host matched.
    /// </summary>
    [Test]
    public async Task HasMatch_PatternWithTrailingStars_ReturnsTrue()
    {
        var match = BypassPatternMatcher.HasMatch(["abc**"], "abc");

        await Assert.That(match).IsTrue();
    }
}
