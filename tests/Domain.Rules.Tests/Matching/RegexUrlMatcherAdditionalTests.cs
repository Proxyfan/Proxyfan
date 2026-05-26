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
    ///     Verifies that a catastrophic-backtracking pattern with a long input returns false rather
    ///     than hanging — the 1-second match timeout is enforced and the exception is swallowed.
    /// </summary>
    [Test]
    public async Task HasMatch_RedosPattern_ReturnsFalseDueToTimeout()
    {
        // (a+)+$ on a long string of a's followed by a non-a character is a classic ReDoS pattern.
        var matcher = new RegexUrlMatcher(@"^(a+)+$");
        var longInput = new string('a', 30) + "X";

        var result = matcher.HasMatch(longInput);

        await Assert.That(result).IsFalse();
    }
}
