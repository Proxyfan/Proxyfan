using CommunityToolkit.Mvvm.ComponentModel;

namespace Proxyfan.Client.Traffic.ViewModels;

/// <summary>
///     View model for a single source group row in the source list panel.
///     Each group represents a unique host (or the synthetic "All" group)
///     together with the count of flows that belong to it.
/// </summary>
public sealed partial class SourceGroupViewModel : ObservableObject
{
    [ObservableProperty]
    private int _count;

    /// <summary>
    ///     Gets the host represented by this group. Empty when this is the
    ///     synthetic "All" group used to clear any host filter.
    /// </summary>
    public string Host { get; }

    /// <summary>
    ///     Gets a value indicating whether this is the synthetic "All" group
    ///     that does not correspond to a specific host.
    /// </summary>
    public bool IsAllGroup { get; }

    /// <summary>
    ///     Initializes a new <see cref="SourceGroupViewModel" /> for the supplied host.
    /// </summary>
    /// <param name="host">The host represented by this group.</param>
    /// <param name="isAllGroup">
    ///     <c>true</c> when this is the synthetic "All" group; otherwise <c>false</c>.
    /// </param>
    public SourceGroupViewModel(string host, bool isAllGroup)
    {
        Host = host;
        IsAllGroup = isAllGroup;
        _count = 0;
    }

    /// <summary>
    ///     Decrements <see cref="Count" /> by one. Does nothing when the
    ///     count is already zero.
    /// </summary>
    public void Decrement()
    {
        if (Count > 0)
        {
            Count--;
        }
    }

    /// <summary>
    ///     Increments <see cref="Count" /> by one.
    /// </summary>
    public void Increment()
    {
        Count++;
    }
}
