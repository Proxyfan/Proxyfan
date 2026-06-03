using System;

namespace Proxyfan.Client.Traffic.ViewModels;

/// <summary>
///     Default in-memory implementation of <see cref="ITrafficListCoordinator" />
///     used to decouple <see cref="SourceListViewModel" /> from
///     <see cref="TrafficListViewModel" />.
/// </summary>
public sealed class TrafficListCoordinator : ITrafficListCoordinator
{
    /// <inheritdoc />
    public event TrafficListCoordinatorHandler? FlowsReset;

    /// <inheritdoc />
    public event TrafficListCoordinatorHandler? HostFilterChanged;

    private string _hostFilter;

    /// <summary>
    ///     Initializes a new <see cref="TrafficListCoordinator" /> with an empty
    ///     host filter.
    /// </summary>
    public TrafficListCoordinator()
    {
        _hostFilter = string.Empty;
    }

    /// <inheritdoc />
    public string HostFilter
    {
        get => _hostFilter;
        set
        {
            var next = value ?? string.Empty;
            if (string.Equals(_hostFilter, next, StringComparison.Ordinal))
            {
                return;
            }

            _hostFilter = next;
            HostFilterChanged?.Invoke();
        }
    }

    /// <inheritdoc />
    public void NotifyFlowsReset()
    {
        FlowsReset?.Invoke();
    }
}
