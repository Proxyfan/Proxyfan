using Proxyfan.Domain.Traffic;

namespace Proxyfan.Client.Inspector.ViewModels;

/// <summary>
///     View model that exposes a single <see cref="TimingPhase" /> for waterfall rendering.
///     Maps the fractional phase position onto a fixed-width lane so that XAML can bind
///     the bar's pixel offset and width directly.
/// </summary>
public sealed class TimingPhaseViewModel
{
    /// <summary>
    ///     The pixel width of the waterfall lane used to project fractional positions onto
    ///     concrete pixel offsets.
    /// </summary>
    public const double LaneWidth = 480d;

    /// <summary>
    ///     Gets the left margin of the bar within the waterfall lane in pixels.
    /// </summary>
    public double BarMarginLeft { get; }

    /// <summary>
    ///     Gets the bar width in pixels (minimum 2 to remain visible).
    /// </summary>
    public double BarWidth { get; }

    /// <summary>
    ///     Gets the human-readable duration label (e.g. <c>"42.50 ms"</c>).
    /// </summary>
    public string DurationText { get; }

    /// <summary>
    ///     Gets the phase name (e.g. <c>"Request"</c>).
    /// </summary>
    public string Name { get; }

    /// <summary>
    ///     Initializes a new instance from a domain <see cref="TimingPhase" />.
    /// </summary>
    /// <param name="phase">The domain phase to project.</param>
    public TimingPhaseViewModel(TimingPhase phase)
    {
        Name = phase.Name;
        DurationText = TimingPhaseDurationFormatter.Format(phase.DurationMilliseconds);
        BarMarginLeft = phase.StartFraction * LaneWidth;
        BarWidth = System.Math.Max(2d, phase.WidthFraction * LaneWidth);
    }
}
