using System.Threading.Tasks;
using Proxyfan.Client.Inspector.ViewModels;
using Proxyfan.Domain.Traffic;

namespace Proxyfan.Client.Tests;

/// <summary>
///     Tests for <see cref="TimingPhaseViewModel" />.
/// </summary>
public sealed class TimingPhaseViewModelTests
{
    /// <summary>
    ///     Verifies that fractional offsets are projected onto the fixed-width lane.
    /// </summary>
    [Test]
    public async Task Constructor_ValidPhase_ProjectsFractionsOntoLane()
    {
        var phase = new TimingPhase("Request", 0.25, 0.5, 100d);

        var viewModel = new TimingPhaseViewModel(phase);

        await Assert.That(viewModel.Name).IsEqualTo("Request");
        await Assert.That(viewModel.BarMarginLeft).IsEqualTo(0.25 * TimingPhaseViewModel.LaneWidth);
        await Assert.That(viewModel.BarWidth).IsEqualTo(0.5 * TimingPhaseViewModel.LaneWidth);
        await Assert.That(viewModel.DurationText).IsEqualTo("100.00 ms");
    }

    /// <summary>
    ///     Verifies that the bar width has a minimum of 2 pixels so zero-width bars remain visible.
    /// </summary>
    [Test]
    public async Task Constructor_TinyWidthFraction_ClampsToMinimum()
    {
        var phase = new TimingPhase("Response", 0d, 0.0001, 0.05);

        var viewModel = new TimingPhaseViewModel(phase);

        await Assert.That(viewModel.BarWidth).IsEqualTo(2d);
    }

    /// <summary>
    ///     Verifies that the duration label is formatted to two decimal places with a
    ///     <c>" ms"</c> suffix using the invariant culture.
    /// </summary>
    [Test]
    public async Task DurationText_FractionalMilliseconds_FormatsAsF2InvariantCulture()
    {
        var phase = new TimingPhase("Waiting", 0.1, 0.2, 42.5);

        var viewModel = new TimingPhaseViewModel(phase);

        await Assert.That(viewModel.DurationText).IsEqualTo("42.50 ms");
    }
}
