using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Proxyfan.Domain.Traffic.Tabs;

/// <summary>
///     The ordered collection of workspace tabs shown above the traffic list. Enforces
///     at least one tab at all times, supports add/close/reorder/activate, and exposes
///     a Changed event whenever the collection or the active tab changes.
/// </summary>
public sealed class TrafficWorkspaceTabSet
{
    /// <summary>
    ///     Raised after the tab collection or the active tab changes.
    /// </summary>
    public event TrafficWorkspaceTabSetChanged? Changed;

    /// <summary>
    ///     The default name applied to the very first tab created when the set is empty.
    /// </summary>
    public const string DefaultFirstTabName = "All Traffic";
    private readonly List<TrafficWorkspaceTab> _tabs;

    /// <summary>
    ///     Gets the currently active tab. Always non-null.
    /// </summary>
    public TrafficWorkspaceTab ActiveTab => _tabs[ActiveTabIndex];

    /// <summary>
    ///     Gets the zero-based index of the currently active tab.
    /// </summary>
    public int ActiveTabIndex { get; private set; }

    /// <summary>
    ///     Gets the number of tabs in the set. Always at least 1.
    /// </summary>
    public int Count => _tabs.Count;

    /// <summary>
    ///     Initializes a new <see cref="TrafficWorkspaceTabSet" /> with a single tab named
    ///     <see cref="DefaultFirstTabName" />.
    /// </summary>
    public TrafficWorkspaceTabSet()
    {
        var firstTab = new TrafficWorkspaceTab(DefaultFirstTabName);
        var tabs = new List<TrafficWorkspaceTab>
        {
            firstTab,
        };
        _tabs = tabs;
        ActiveTabIndex = 0;
    }

    /// <summary>
    ///     Initializes a new <see cref="TrafficWorkspaceTabSet" /> with the specified initial tab.
    /// </summary>
    /// <param name="initialTab">The first tab. Cannot be null.</param>
    public TrafficWorkspaceTabSet(TrafficWorkspaceTab initialTab)
    {
        var tabs = new List<TrafficWorkspaceTab>
        {
            initialTab,
        };
        _tabs = tabs;
        ActiveTabIndex = 0;
    }

    /// <summary>
    ///     Activates the tab at <paramref name="index" />. Out-of-range indices are ignored.
    /// </summary>
    /// <param name="index">The zero-based index of the tab to activate.</param>
    public void Activate(int index)
    {
        if (index < 0 || index >= _tabs.Count)
        {
            return;
        }

        if (ActiveTabIndex == index)
        {
            return;
        }

        ActiveTabIndex = index;
        Changed?.Invoke(this);
    }

    /// <summary>
    ///     Appends <paramref name="tab" /> to the end of the set and activates it.
    /// </summary>
    /// <param name="tab">The tab to add.</param>
    public void Add(TrafficWorkspaceTab tab)
    {
        _tabs.Add(tab);
        ActiveTabIndex = _tabs.Count - 1;
        Changed?.Invoke(this);
    }

    /// <summary>
    ///     Closes the tab at <paramref name="index" />. Does nothing when the set has only
    ///     one tab (the last tab cannot be closed) or when the index is out of range.
    ///     Adjusts the active tab to the closest valid neighbour when the active tab is closed.
    /// </summary>
    /// <param name="index">The zero-based index of the tab to close.</param>
    public void Close(int index)
    {
        if (_tabs.Count <= 1)
        {
            return;
        }

        if (index < 0 || index >= _tabs.Count)
        {
            return;
        }

        _tabs.RemoveAt(index);
        if (ActiveTabIndex > index)
        {
            ActiveTabIndex--;
        }
        else if (ActiveTabIndex >= _tabs.Count)
        {
            ActiveTabIndex = _tabs.Count - 1;
        }

        Changed?.Invoke(this);
    }

    /// <summary>
    ///     Moves the tab from <paramref name="fromIndex" /> to <paramref name="toIndex" />.
    ///     Out-of-range or no-op moves are ignored. The active tab follows its original tab.
    /// </summary>
    /// <param name="fromIndex">The current zero-based index of the tab.</param>
    /// <param name="toIndex">The destination zero-based index.</param>
    public void Move(int fromIndex, int toIndex)
    {
        if (fromIndex < 0 || fromIndex >= _tabs.Count)
        {
            return;
        }

        if (toIndex < 0 || toIndex >= _tabs.Count)
        {
            return;
        }

        if (fromIndex == toIndex)
        {
            return;
        }

        var tab = _tabs[fromIndex];
        _tabs.RemoveAt(fromIndex);
        _tabs.Insert(toIndex, tab);

        if (ActiveTabIndex == fromIndex)
        {
            ActiveTabIndex = toIndex;
        }
        else if (fromIndex < ActiveTabIndex && toIndex >= ActiveTabIndex)
        {
            ActiveTabIndex--;
        }
        else if (fromIndex > ActiveTabIndex && toIndex <= ActiveTabIndex)
        {
            ActiveTabIndex++;
        }

        Changed?.Invoke(this);
    }

    /// <summary>
    ///     Returns a read-only snapshot of the current tabs in order.
    /// </summary>
    /// <returns>
    ///     A new read-only collection of <see cref="TrafficWorkspaceTab" />.
    /// </returns>
    public ReadOnlyCollection<TrafficWorkspaceTab> Snapshot()
    {
        var array = new TrafficWorkspaceTab[_tabs.Count];
        for (var index = 0; index < _tabs.Count; index++)
        {
            array[index] = _tabs[index];
        }

        var snapshot = new ReadOnlyCollection<TrafficWorkspaceTab>(array);
        return snapshot;
    }
}
