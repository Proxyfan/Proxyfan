using System;

namespace Proxyfan.Domain.Traffic.Tabs;

/// <summary>
///     The mutable per-tab state for a single workspace tab. Tabs share the same
///     traffic store but maintain independent filter queries, names, and selection.
/// </summary>
public sealed class TrafficWorkspaceTab
{
    /// <summary>
    ///     Raised after any property on the tab changes (name, filter query, or selection).
    /// </summary>
    public event TrafficWorkspaceTabChanged? Changed;

    /// <summary>
    ///     Gets the case-insensitive filter query applied to the traffic list for this tab.
    ///     Empty when no filter is active.
    /// </summary>
    public string FilterQuery { get; private set; }

    /// <summary>
    ///     Gets the stable identifier for this tab. Generated once and used by UI layouts
    ///     and configuration to track the tab across rename/reorder.
    /// </summary>
    public Guid Id { get; }

    /// <summary>
    ///     Gets the user-visible tab name. Always non-empty.
    /// </summary>
    public string Name { get; private set; }

    /// <summary>
    ///     Gets the id of the currently-selected flow on this tab, or <c>null</c> when no
    ///     row is selected.
    /// </summary>
    public Guid? SelectedFlowId { get; private set; }

    /// <summary>
    ///     Initializes a new tab with the specified <paramref name="name" />, an empty
    ///     filter, and no selection.
    /// </summary>
    /// <param name="name">The user-visible tab name. Must be non-empty.</param>
    /// <exception cref="ArgumentException">
    ///     Thrown when <paramref name="name" /> is empty or whitespace.
    /// </exception>
    public TrafficWorkspaceTab(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Tab name must not be empty or whitespace.", nameof(name));
        }

        Id = Guid.NewGuid();
        Name = name;
        FilterQuery = string.Empty;
        SelectedFlowId = null;
    }

    /// <summary>
    ///     Replaces the active filter query. Whitespace and null are normalised to
    ///     empty string.
    /// </summary>
    /// <param name="query">The new filter query.</param>
    public void SetFilterQuery(string? query)
    {
        var normalised = query ?? string.Empty;
        if (string.Equals(FilterQuery, normalised, StringComparison.Ordinal))
        {
            return;
        }

        FilterQuery = normalised;
        Changed?.Invoke(this);
    }

    /// <summary>
    ///     Renames the tab.
    /// </summary>
    /// <param name="name">The new tab name. Must be non-empty.</param>
    /// <exception cref="ArgumentException">
    ///     Thrown when <paramref name="name" /> is empty or whitespace.
    /// </exception>
    public void SetName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Tab name must not be empty or whitespace.", nameof(name));
        }

        if (string.Equals(Name, name, StringComparison.Ordinal))
        {
            return;
        }

        Name = name;
        Changed?.Invoke(this);
    }

    /// <summary>
    ///     Sets the selected flow id, or clears it when <paramref name="flowId" /> is null.
    /// </summary>
    /// <param name="flowId">The id of the selected flow, or null to clear the selection.</param>
    public void SetSelectedFlowId(Guid? flowId)
    {
        if (SelectedFlowId == flowId)
        {
            return;
        }

        SelectedFlowId = flowId;
        Changed?.Invoke(this);
    }
}
