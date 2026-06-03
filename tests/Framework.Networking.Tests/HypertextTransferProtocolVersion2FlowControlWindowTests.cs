using System.Threading.Tasks;

namespace Proxyfan.Framework.Networking.Tests;

/// <summary>
///     Tests for <see cref="HypertextTransferProtocolVersion2FlowControlWindow" />.
/// </summary>
public sealed class HypertextTransferProtocolVersion2FlowControlWindowTests
{
    /// <summary>
    ///     A default-constructed window has the RFC 7540 § 6.9.2 initial size of 65 535 octets.
    /// </summary>
    [Test]
    public async Task Available_DefaultConstructor_IsSixtyFiveThousandFiveHundredThirtyFive()
    {
        var window = new HypertextTransferProtocolVersion2FlowControlWindow();

        await Assert.That(window.Available).IsEqualTo(65535);
    }

    /// <summary>
    ///     <see cref="HypertextTransferProtocolVersion2FlowControlWindow.HasConsumed" /> succeeds
    ///     when the request fits in the budget.
    /// </summary>
    [Test]
    public async Task HasConsumed_SufficientBudget_ReturnsTrueAndDecrements()
    {
        var window = new HypertextTransferProtocolVersion2FlowControlWindow(100);

        var result = window.HasConsumed(40);

        await Assert.That(result).IsTrue();
        await Assert.That(window.Available).IsEqualTo(60);
    }

    /// <summary>
    ///     A consume that exceeds the available budget is rejected without mutating the window.
    /// </summary>
    [Test]
    public async Task HasConsumed_InsufficientBudget_ReturnsFalseAndLeavesWindowUnchanged()
    {
        var window = new HypertextTransferProtocolVersion2FlowControlWindow(10);

        var result = window.HasConsumed(20);

        await Assert.That(result).IsFalse();
        await Assert.That(window.Available).IsEqualTo(10);
    }

    /// <summary>
    ///     A WINDOW_UPDATE increment of zero is a stream-level protocol error per RFC 7540 § 6.9 and is rejected.
    /// </summary>
    [Test]
    public async Task HasIncremented_ZeroIncrement_ReturnsFalse()
    {
        var window = new HypertextTransferProtocolVersion2FlowControlWindow(0);

        var result = window.HasIncremented(0);

        await Assert.That(result).IsFalse();
    }

    /// <summary>
    ///     A positive increment within the maximum budget extends the window.
    /// </summary>
    [Test]
    public async Task HasIncremented_PositiveIncrement_ExtendsWindow()
    {
        var window = new HypertextTransferProtocolVersion2FlowControlWindow(100);

        var result = window.HasIncremented(50);

        await Assert.That(result).IsTrue();
        await Assert.That(window.Available).IsEqualTo(150);
    }

    /// <summary>
    ///     An increment that would push the window above 2^31 - 1 is a FLOW_CONTROL_ERROR per
    ///     RFC 7540 § 6.9.1 and is rejected.
    /// </summary>
    [Test]
    public async Task HasIncremented_AboveMaximumSize_ReturnsFalse()
    {
        var window = new HypertextTransferProtocolVersion2FlowControlWindow(HypertextTransferProtocolVersion2FlowControlWindow.MaximumSize - 1);

        var result = window.HasIncremented(10);

        await Assert.That(result).IsFalse();
        await Assert.That(window.Available).IsEqualTo(HypertextTransferProtocolVersion2FlowControlWindow.MaximumSize - 1);
    }

    /// <summary>
    ///     <see cref="HypertextTransferProtocolVersion2FlowControlWindow.HasAppliedInitialSizeDelta" />
    ///     shifts the window by the SETTINGS-derived delta and may temporarily go negative.
    /// </summary>
    [Test]
    public async Task HasAppliedInitialSizeDelta_NegativeDelta_AllowsNegativeAvailable()
    {
        var window = new HypertextTransferProtocolVersion2FlowControlWindow(100);

        var result = window.HasAppliedInitialSizeDelta(-200);

        await Assert.That(result).IsTrue();
        await Assert.That(window.Available).IsEqualTo(-100);
    }

    /// <summary>
    ///     A delta that pushes the window above the maximum returns false (FLOW_CONTROL_ERROR per
    ///     RFC 7540 § 6.9.2) and leaves the window unchanged.
    /// </summary>
    [Test]
    public async Task HasAppliedInitialSizeDelta_DeltaExceedsMaximum_ReturnsFalseAndLeavesWindowUnchanged()
    {
        var window = new HypertextTransferProtocolVersion2FlowControlWindow(HypertextTransferProtocolVersion2FlowControlWindow.MaximumSize - 10);

        var result = window.HasAppliedInitialSizeDelta(int.MaxValue);

        await Assert.That(result).IsFalse();
        await Assert.That(window.Available).IsEqualTo(HypertextTransferProtocolVersion2FlowControlWindow.MaximumSize - 10);
    }

    /// <summary>
    ///     A delta that would push the window below <see cref="int.MinValue" /> returns false
    ///     and leaves the window unchanged rather than overflowing.
    /// </summary>
    [Test]
    public async Task HasAppliedInitialSizeDelta_BelowMinimum_ReturnsFalseAndLeavesWindowUnchanged()
    {
        var window = new HypertextTransferProtocolVersion2FlowControlWindow(0);
        var firstShift = window.HasAppliedInitialSizeDelta(int.MinValue);

        var result = window.HasAppliedInitialSizeDelta(-1);

        await Assert.That(firstShift).IsTrue();
        await Assert.That(result).IsFalse();
        await Assert.That(window.Available).IsEqualTo(int.MinValue);
    }
}
