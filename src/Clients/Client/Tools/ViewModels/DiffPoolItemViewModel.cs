using Proxyfan.Domain.Traffic;
using System;

namespace Proxyfan.Client.Tools.ViewModels;

/// <summary>
///     A read-only projection of a <see cref="TrafficFlow" /> for selection in the
///     Diff Tool window's left/right pickers.
/// </summary>
public sealed class DiffPoolItemViewModel
{
    /// <summary>
    ///     Gets the human-readable display string combining method, URL and status code.
    /// </summary>
    public string DisplayName { get; }

    /// <summary>
    ///     Gets the underlying domain flow snapshot.
    /// </summary>
    public TrafficFlow Flow { get; }

    /// <summary>
    ///     Gets the UTC instant at which the flow started.
    /// </summary>
    public DateTimeOffset StartedAt { get; }

    /// <summary>
    ///     Initializes a new <see cref="DiffPoolItemViewModel" /> from a domain flow.
    /// </summary>
    /// <param name="flow">The flow this row represents.</param>
    public DiffPoolItemViewModel(TrafficFlow flow)
    {
        Flow = flow;
        StartedAt = flow.StartedAt;
        DisplayName = DiffPoolItemDisplayFormatter.Format(flow);
    }
}
