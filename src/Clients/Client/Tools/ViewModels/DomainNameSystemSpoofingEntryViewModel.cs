using CommunityToolkit.Mvvm.ComponentModel;
using Proxyfan.Domain.DomainNameSystemSpoofing;

namespace Proxyfan.Client.Tools.ViewModels;

/// <summary>
///     View model for a single DNS override entry, exposing the host name, target IP
///     address, pattern kind, enabled state, and match counter backed by the underlying
///     <see cref="DomainNameSystemOverrideEntry" />.
/// </summary>
public sealed partial class DomainNameSystemSpoofingEntryViewModel : ObservableObject
{
    [ObservableProperty]
    private bool _isEnabled;
    [ObservableProperty]
    private int _matchCount;

    /// <summary>
    ///     Gets the underlying domain entry.
    /// </summary>
    public DomainNameSystemOverrideEntry Entry { get; }

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
    ///     Initializes a new <see cref="DomainNameSystemSpoofingEntryViewModel" /> wrapping
    ///     the supplied entry. Initial property values mirror the entry's state at the time
    ///     of construction.
    /// </summary>
    /// <param name="entry">The domain entry to expose.</param>
    public DomainNameSystemSpoofingEntryViewModel(DomainNameSystemOverrideEntry entry)
    {
        Entry = entry;
        Hostname = entry.Hostname;
        OverrideAddress = entry.OverrideAddress.ToString();
        KindDisplay = entry.Kind == DomainOverrideKind.WildcardSuffix ? "Wildcard" : "Exact";
        _isEnabled = entry.IsEnabled;
        _matchCount = entry.MatchCount;
    }

    /// <summary>
    ///     Synchronises this view model's <see cref="MatchCount" /> property from the
    ///     underlying entry. Should be invoked on the UI thread.
    /// </summary>
    public void RefreshMatchCount()
    {
        var current = Entry.MatchCount;
        if (MatchCount != current)
        {
            MatchCount = current;
        }
    }

    partial void OnIsEnabledChanged(bool value)
    {
        if (Entry.IsEnabled != value)
        {
            Entry.IsEnabled = value;
        }
    }
}
