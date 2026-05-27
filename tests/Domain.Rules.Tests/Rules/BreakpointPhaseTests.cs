using Proxyfan.Domain.Rules.Rules;
using System.Threading.Tasks;

namespace Proxyfan.Domain.Rules.Tests.Rules;

/// <summary>
///     Tests for <see cref="BreakpointPhase" />.
/// </summary>
public sealed class BreakpointPhaseTests
{
    /// <summary>
    ///     Verifies that <see cref="BreakpointPhase.Both" /> is the bitwise union of Request and Response.
    /// </summary>
    [Test]
    public async Task Both_HasFlag_IncludesBothPhases()
    {
        await Assert.That(BreakpointPhase.Both.HasFlag(BreakpointPhase.Request)).IsTrue();
        await Assert.That(BreakpointPhase.Both.HasFlag(BreakpointPhase.Response)).IsTrue();
    }

    /// <summary>
    ///     Verifies that <see cref="BreakpointPhase.None" /> includes neither phase.
    /// </summary>
    [Test]
    public async Task None_HasFlag_IncludesNeitherPhase()
    {
        await Assert.That(BreakpointPhase.None.HasFlag(BreakpointPhase.Request)).IsFalse();
        await Assert.That(BreakpointPhase.None.HasFlag(BreakpointPhase.Response)).IsFalse();
    }
}
