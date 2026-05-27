namespace Proxyfan.Domain.Traffic;

/// <summary>
///     User-assigned color tag for organising and annotating traffic flows during a
///     debugging session. Colours appear as small dots in the traffic list and are
///     filterable. The default value, <see cref="None" />, indicates that no colour
///     has been assigned by the user.
/// </summary>
public enum TrafficFlowColorTag
{
    /// <summary>
    ///     No colour assigned (the default).
    /// </summary>
    None = 0,

    /// <summary>
    ///     Red — typically used for errors or failures the user wants to revisit.
    /// </summary>
    Red = 1,

    /// <summary>
    ///     Orange — secondary warning colour.
    /// </summary>
    Orange = 2,

    /// <summary>
    ///     Yellow — caution / inspection-pending.
    /// </summary>
    Yellow = 3,

    /// <summary>
    ///     Green — known-good or successful flows.
    /// </summary>
    Green = 4,

    /// <summary>
    ///     Blue — informational marker.
    /// </summary>
    Blue = 5,

    /// <summary>
    ///     Purple — secondary informational marker.
    /// </summary>
    Purple = 6,

    /// <summary>
    ///     Gray — muted / archived flows.
    /// </summary>
    Gray = 7,
}
