using Proxyfan.Domain.Rules.Matching;
using Proxyfan.Domain.Rules.Rules;
using System;
using System.Threading.Tasks;

namespace Proxyfan.Domain.Rules.Tests;

/// <summary>
///     Tests for <see cref="MutableBreakpointConfiguration" />.
/// </summary>
public sealed class MutableBreakpointConfigurationTests
{
    /// <summary>
    ///     A disabled configuration never matches even when a pattern is registered.
    /// </summary>
    [Test]
    public async Task HasRequestMatch_Disabled_ReturnsFalse()
    {
        var configuration = new MutableBreakpointConfiguration(isEnabled: false);
        configuration.AddPattern(new MatchingRule("*", MatchingRuleKind.Wildcard));

        var hasMatch = configuration.HasRequestMatch("https://example.com/");

        await Assert.That(hasMatch).IsFalse();
    }

    /// <summary>
    ///     An enabled configuration with no patterns returns false.
    /// </summary>
    [Test]
    public async Task HasRequestMatch_NoPatterns_ReturnsFalse()
    {
        var configuration = new MutableBreakpointConfiguration(isEnabled: true);

        var hasMatch = configuration.HasRequestMatch("https://example.com/");

        await Assert.That(hasMatch).IsFalse();
    }

    /// <summary>
    ///     A request-phase match succeeds when a matching pattern exists.
    /// </summary>
    [Test]
    public async Task HasRequestMatch_MatchingPattern_ReturnsTrue()
    {
        var configuration = new MutableBreakpointConfiguration(isEnabled: true);
        configuration.AddPattern(new MatchingRule("https://example.com/*", MatchingRuleKind.Wildcard));

        var hasMatch = configuration.HasRequestMatch("https://example.com/api");

        await Assert.That(hasMatch).IsTrue();
    }

    /// <summary>
    ///     A request-phase match returns false when only the response phase is selected.
    /// </summary>
    [Test]
    public async Task HasRequestMatch_PhaseResponseOnly_ReturnsFalse()
    {
        var configuration = new MutableBreakpointConfiguration(isEnabled: true);
        configuration.SetPhases(BreakpointPhase.Response);
        configuration.AddPattern(new MatchingRule("*", MatchingRuleKind.Wildcard));

        var hasMatch = configuration.HasRequestMatch("https://example.com/");

        await Assert.That(hasMatch).IsFalse();
    }

    /// <summary>
    ///     A response-phase match returns false when only the request phase is selected.
    /// </summary>
    [Test]
    public async Task HasResponseMatch_PhaseRequestOnly_ReturnsFalse()
    {
        var configuration = new MutableBreakpointConfiguration(isEnabled: true);
        configuration.SetPhases(BreakpointPhase.Request);
        configuration.AddPattern(new MatchingRule("*", MatchingRuleKind.Wildcard));

        var hasMatch = configuration.HasResponseMatch("https://example.com/");

        await Assert.That(hasMatch).IsFalse();
    }

    /// <summary>
    ///     A response-phase match succeeds when a matching pattern exists.
    /// </summary>
    [Test]
    public async Task HasResponseMatch_MatchingPattern_ReturnsTrue()
    {
        var configuration = new MutableBreakpointConfiguration(isEnabled: true);
        configuration.AddPattern(new MatchingRule("*", MatchingRuleKind.Wildcard));

        var hasMatch = configuration.HasResponseMatch("https://example.com/api");

        await Assert.That(hasMatch).IsTrue();
    }

    /// <summary>
    ///     Adding a pattern raises the Changed event.
    /// </summary>
    [Test]
    public async Task AddPattern_NewPattern_RaisesChanged()
    {
        var configuration = new MutableBreakpointConfiguration(isEnabled: true);
        var count = 0;
        configuration.Changed += () => count++;

        configuration.AddPattern(new MatchingRule("*", MatchingRuleKind.Wildcard));

        await Assert.That(count).IsEqualTo(1);
    }

    /// <summary>
    ///     Adding a duplicate pattern is ignored and does not raise Changed.
    /// </summary>
    [Test]
    public async Task AddPattern_Duplicate_DoesNotRaiseChanged()
    {
        var configuration = new MutableBreakpointConfiguration(isEnabled: true);
        var pattern = new MatchingRule("*", MatchingRuleKind.Wildcard);
        configuration.AddPattern(pattern);
        var count = 0;
        configuration.Changed += () => count++;

        configuration.AddPattern(pattern);

        await Assert.That(count).IsEqualTo(0);
        await Assert.That(configuration.GetPatterns().Count).IsEqualTo(1);
    }

    /// <summary>
    ///     Removing a registered pattern raises Changed and removes it from snapshots.
    /// </summary>
    [Test]
    public async Task RemovePattern_RegisteredPattern_RaisesChanged()
    {
        var configuration = new MutableBreakpointConfiguration(isEnabled: true);
        var pattern = new MatchingRule("*", MatchingRuleKind.Wildcard);
        configuration.AddPattern(pattern);
        var count = 0;
        configuration.Changed += () => count++;

        configuration.RemovePattern(pattern);

        await Assert.That(count).IsEqualTo(1);
        await Assert.That(configuration.GetPatterns().Count).IsEqualTo(0);
    }

    /// <summary>
    ///     Removing an unknown pattern does not raise Changed.
    /// </summary>
    [Test]
    public async Task RemovePattern_UnknownPattern_DoesNotRaiseChanged()
    {
        var configuration = new MutableBreakpointConfiguration(isEnabled: true);
        var count = 0;
        configuration.Changed += () => count++;

        configuration.RemovePattern(new MatchingRule("*", MatchingRuleKind.Wildcard));

        await Assert.That(count).IsEqualTo(0);
    }

    /// <summary>
    ///     Removing a pattern that does not match any existing entry iterates the full
    ///     pattern list and exits the loop naturally (covers the post-foreach branch).
    /// </summary>
    [Test]
    public async Task RemovePattern_NotMatchingExistingPatterns_DoesNotRemoveAny()
    {
        var configuration = new MutableBreakpointConfiguration(isEnabled: true);
        configuration.AddPattern(new MatchingRule("https://kept.example/*", MatchingRuleKind.Wildcard));
        var count = 0;
        configuration.Changed += () => count++;

        configuration.RemovePattern(new MatchingRule("https://other.example/*", MatchingRuleKind.Wildcard));

        await Assert.That(count).IsEqualTo(0);
        await Assert.That(configuration.HasRequestMatch("https://kept.example/path")).IsTrue();
    }

    /// <summary>
    ///     SetEnabled changes the IsEnabled property and raises Changed.
    /// </summary>
    [Test]
    public async Task SetEnabled_TogglesValue_RaisesChanged()
    {
        var configuration = new MutableBreakpointConfiguration(isEnabled: false);
        var count = 0;
        configuration.Changed += () => count++;

        configuration.SetEnabled(isEnabled: true);

        await Assert.That(configuration.IsEnabled).IsTrue();
        await Assert.That(count).IsEqualTo(1);
    }

    /// <summary>
    ///     SetEnabled with the same value does not raise Changed.
    /// </summary>
    [Test]
    public async Task SetEnabled_NoChange_DoesNotRaiseChanged()
    {
        var configuration = new MutableBreakpointConfiguration(isEnabled: true);
        var count = 0;
        configuration.Changed += () => count++;

        configuration.SetEnabled(isEnabled: true);

        await Assert.That(count).IsEqualTo(0);
    }

    /// <summary>
    ///     SetPhases changes the Phases property and raises Changed.
    /// </summary>
    [Test]
    public async Task SetPhases_NewValue_RaisesChanged()
    {
        var configuration = new MutableBreakpointConfiguration(isEnabled: true);
        var count = 0;
        configuration.Changed += () => count++;

        configuration.SetPhases(BreakpointPhase.Request);

        await Assert.That(configuration.Phases).IsEqualTo(BreakpointPhase.Request);
        await Assert.That(count).IsEqualTo(1);
    }

    /// <summary>
    ///     SetPhases with the same value does not raise Changed.
    /// </summary>
    [Test]
    public async Task SetPhases_NoChange_DoesNotRaiseChanged()
    {
        var configuration = new MutableBreakpointConfiguration(isEnabled: true);
        var count = 0;
        configuration.Changed += () => count++;

        configuration.SetPhases(BreakpointPhase.Both);

        await Assert.That(count).IsEqualTo(0);
    }

    /// <summary>
    ///     GetPatterns returns a defensive snapshot.
    /// </summary>
    [Test]
    public async Task GetPatterns_AfterMutation_ReturnsDefensiveSnapshot()
    {
        var configuration = new MutableBreakpointConfiguration(isEnabled: true);
        configuration.AddPattern(new MatchingRule("a", MatchingRuleKind.Exact));

        var snapshot = configuration.GetPatterns();
        configuration.AddPattern(new MatchingRule("b", MatchingRuleKind.Exact));

        await Assert.That(snapshot.Count).IsEqualTo(1);
        await Assert.That(configuration.GetPatterns().Count).IsEqualTo(2);
    }

    /// <summary>
    ///     A malformed pattern whose compilation throws must not be added, and
    ///     existing matchers must remain intact.
    /// </summary>
    [Test]
    public async Task AddPattern_CompilationThrows_DoesNotPoisonConfiguration()
    {
        var configuration = new MutableBreakpointConfiguration(isEnabled: true);
        configuration.AddPattern(new MatchingRule("https://example.com/*", MatchingRuleKind.Wildcard));
        var changedCount = 0;
        configuration.Changed += () => changedCount++;

        Exception? caught = null;
        try
        {
            configuration.AddPattern(new MatchingRule("(", MatchingRuleKind.Regex));
        }
        catch (Exception exception)
        {
            caught = exception;
        }

        await Assert.That(caught).IsNotNull();
        await Assert.That(configuration.GetPatterns().Count).IsEqualTo(1);
        await Assert.That(configuration.HasRequestMatch("https://example.com/api")).IsTrue();
        await Assert.That(changedCount).IsEqualTo(0);
    }

    /// <summary>
    ///     Constructor initializes pause-queue safeguards with their documented defaults.
    /// </summary>
    [Test]
    public async Task Constructor_Defaults_ExposePauseSafeguards()
    {
        var configuration = new MutableBreakpointConfiguration(isEnabled: true);

        await Assert.That(configuration.MaxPendingPauses).IsEqualTo(100);
        await Assert.That(configuration.PauseTimeout).IsEqualTo(TimeSpan.FromSeconds(60));
        await Assert.That(configuration.IsBackPressureEnabled).IsFalse();
    }

    /// <summary>
    ///     Safeguard setters update their properties and raise Changed.
    /// </summary>
    [Test]
    public async Task SafeguardSetters_NewValues_RaiseChanged()
    {
        var configuration = new MutableBreakpointConfiguration(isEnabled: true);
        var changed = 0;
        configuration.Changed += () => changed++;

        configuration.SetMaxPendingPauses(200);
        configuration.SetPauseTimeout(TimeSpan.FromSeconds(5));
        configuration.SetBackPressureEnabled(isBackPressureEnabled: true);

        await Assert.That(configuration.MaxPendingPauses).IsEqualTo(200);
        await Assert.That(configuration.PauseTimeout).IsEqualTo(TimeSpan.FromSeconds(5));
        await Assert.That(configuration.IsBackPressureEnabled).IsTrue();
        await Assert.That(changed).IsEqualTo(3);
    }

    /// <summary>
    ///     Invalid safeguard values are rejected.
    /// </summary>
    [Test]
    public async Task SafeguardSetters_InvalidValues_Throws()
    {
        var configuration = new MutableBreakpointConfiguration(isEnabled: true);

        await Assert.That(() => configuration.SetMaxPendingPauses(0)).Throws<ArgumentOutOfRangeException>();
        await Assert.That(() => configuration.SetPauseTimeout(TimeSpan.Zero)).Throws<ArgumentOutOfRangeException>();
    }
}
