using CommunityToolkit.Mvvm.ComponentModel;
using Proxyfan.Domain.DomainNameSystemSpoofing;

namespace Proxyfan.Client.Tools.ViewModels;

/// <summary>
///     Immutable projection of a single DNS override entry for binding. Display
///     properties (<see cref="Hostname" />, <see cref="OverrideAddress" />,
///     <see cref="KindDisplay" />, <see cref="CanonicalPattern" />) are snapshotted
///     at construction and never change. The mutable <see cref="IsEnabled" /> and
///     <see cref="MatchCount" /> surfaces are kept in sync with the owning
///     <see cref="DomainNameSystemOverrideMap" /> via an explicit update path: the
///     parent supplies a <see cref="DomainNameSystemSpoofingEntryIsEnabledChanged" />
///     callback that this row invokes whenever the checkbox-bound
///     <see cref="IsEnabled" /> setter runs, so all writes flow through a single
///     map-level mutation method.
/// </summary>
public sealed partial class DomainNameSystemSpoofingEntryViewModel : ObservableObject
{
    private readonly DomainNameSystemSpoofingEntryIsEnabledChanged _onIsEnabledChanged;
    [ObservableProperty]
    private bool _isEnabled;
    [ObservableProperty]
    private int _matchCount;

    /// <summary>
    ///     Gets the canonical (lower-case, trimmed) pattern identifying this entry in
    ///     the owning map. Used by the parent view model to route updates back to a
    ///     single map-level mutation method.
    /// </summary>
    public string CanonicalPattern { get; }

    /// <summary>
    ///     Gets the host name (or wildcard pattern) being overridden.
    /// </summary>
    public string Hostname { get; }

    /// <summary>
    ///     Gets the kind of pattern as a localizable display string.
    /// </summary>
    public string KindDisplay { get; }

    /// <summary>
    ///     Gets the override IP address as a display string.
    /// </summary>
    public string OverrideAddress { get; }

    /// <summary>
    ///     Initializes a new <see cref="DomainNameSystemSpoofingEntryViewModel" />
    ///     projecting the supplied entry. Display properties mirror the entry's state
    ///     at the time of construction. <paramref name="onIsEnabledChanged" /> is
    ///     invoked whenever the bindable <see cref="IsEnabled" /> setter runs from the
    ///     UI, and is the only path through which the underlying map is updated.
    /// </summary>
    /// <param name="entry">The domain entry to project.</param>
    /// <param name="onIsEnabledChanged">
    ///     Callback raised on the UI thread whenever the bindable
    ///     <see cref="IsEnabled" /> changes. The parent view model wires this to a
    ///     map-level mutation method.
    /// </param>
    public DomainNameSystemSpoofingEntryViewModel(
        DomainNameSystemOverrideEntry entry,
        DomainNameSystemSpoofingEntryIsEnabledChanged onIsEnabledChanged)
    {
        _onIsEnabledChanged = onIsEnabledChanged;
        CanonicalPattern = entry.CanonicalPattern;
        Hostname = entry.Hostname;
        OverrideAddress = entry.OverrideAddress.ToString();
        KindDisplay = entry.Kind == DomainOverrideKind.WildcardSuffix ? "Wildcard" : "Exact";
        _isEnabled = entry.IsEnabled;
        _matchCount = entry.MatchCount;
    }

    /// <summary>
    ///     Updates the displayed match count to <paramref name="value" /> without
    ///     re-entering the property-changed pipeline when the value is unchanged.
    ///     Invoked by the parent view model after polling the underlying map. Should
    ///     be invoked on the UI thread.
    /// </summary>
    /// <param name="value">The new match-count value to surface.</param>
    public void UpdateMatchCount(int value)
    {
        if (MatchCount != value)
        {
            MatchCount = value;
        }
    }

    partial void OnIsEnabledChanged(bool value)
    {
        _onIsEnabledChanged(value);
    }
}
