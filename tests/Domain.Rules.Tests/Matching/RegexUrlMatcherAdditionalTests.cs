using Proxyfan.Domain.Rules.Matching;
using System.Threading.Tasks;

namespace Proxyfan.Domain.Rules.Tests.Matching;

/// <summary>
///     Additional tests for <see cref="RegexUrlMatcher" /> covering the ReDoS timeout and
///     remaining branches.
/// </summary>
public sealed class RegexUrlMatcherAdditionalTests
{
    /// <summary>
    ///     Verifies that a catastrophic-backtracking pattern with a long input reports an
    ///     indeterminate evaluation rather than being treated as a non-match.
    /// </summary>
    [Test]
    public async Task GetMatchResult_RedosPattern_ReturnsIndeterminate()
    {
        // (a+)+$ on a long string of a's followed by a non-a character is a classic ReDoS pattern.
        var matcher = new RegexUrlMatcher(@"^(a+)+$");
        var longInput = new string('a', 30) + "X";

        var result = matcher.GetMatchResult(longInput);

        await Assert.That(result).IsEqualTo(UrlMatchResult.Indeterminate);
    }
}
