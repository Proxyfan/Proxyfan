namespace Proxyfan.Domain.Traffic;

/// <summary>
///     Represents a single segment of a flow's timing waterfall.
/// </summary>
public sealed record TimingPhase
{
    /// <summary>
    ///     Gets the phase duration in milliseconds.
    /// </summary>
    public double DurationMilliseconds { get; }

    /// <summary>
    ///     Gets the phase name.
    /// </summary>
    public string Name { get; }

    /// <summary>
    ///     Gets the phase start as a fraction of the total flow duration.
    /// </summary>
    public double StartFraction { get; }

    /// <summary>
    ///     Gets the phase duration as a fraction of the total flow duration.
    /// </summary>
    public double WidthFraction { get; }

    /// <summary>
    ///     Initializes a new <see cref="TimingPhase" />.
    /// </summary>
    /// <param name="name">The phase name (e.g. <c>Request</c>, <c>Waiting</c>, <c>Response</c>).</param>
    /// <param name="startFraction">
    ///     The phase start as a fraction of the total flow duration in the range <c>[0, 1]</c>.
    /// </param>
    /// <param name="widthFraction">
    ///     The phase duration as a fraction of the total flow duration in the range <c>(0, 1]</c>.
    /// </param>
    /// <param name="durationMilliseconds">The phase duration in milliseconds.</param>
    public TimingPhase(string name, double startFraction, double widthFraction, double durationMilliseconds)
    {
        Name = name;
        StartFraction = startFraction;
        WidthFraction = widthFraction;
        DurationMilliseconds = durationMilliseconds;
    }
}
