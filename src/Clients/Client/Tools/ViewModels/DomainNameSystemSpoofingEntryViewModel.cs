using CommunityToolkit.Mvvm.ComponentModel;
using Proxyfan.Domain.DomainNameSystemSpoofing;

namespace Proxyfan.Client.Tools.ViewModels;

/// <summary>
///     Immutable row projection of a single DNS override entry. Exposes the host name,
///     target IP address, pattern kind, enabled state, and match counter for binding,
///     but does not mutate the underlying <see cref="DomainNameSystemOverrideEntry" />
///     directly. Enabled-state changes from the UI are routed through the parent
///     callback supplied at construction so the owning view model can funnel every
///     mutation through <see cref="DomainNameSystemOverrideMap" />.
/// </summary>
public sealed partial class DomainNameSystemSpoofingEntryViewModel : ObservableObject
{
    private readonly DomainNameSystemSpoofingEntryEnabledChanged _onIsEnabledChanged;
    [ObservableProperty]
    private bool _isEnabled;
    private bool _isSuppressingEnabledCallback;
    [ObservableProperty]
    private int _matchCount;

    /// <summary>
    ///     Gets the canonical (lower-case, trimmed, trailing-dot stripped) pattern used
    ///     by the map to identify this entry.
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
    ///     Initializes a new <see cref="DomainNameSystemSpoofingEntryViewModel" /> that
    ///     projects <paramref name="entry" />'s state at construction time. Subsequent
    ///     <see cref="IsEnabled" /> changes raised by binding are forwarded to
    ///     <paramref name="onIsEnabledChanged" />; changes pushed back from the map via
    ///     <see cref="SetIsEnabledFromMap" /> do not re-enter the callback.
    /// </summary>
    /// <param name="entry">The domain entry to project.</param>
    /// <param name="onIsEnabledChanged">
    ///     Callback invoked when the bound <see cref="IsEnabled" /> property changes,
    ///     receiving the row and the new value.
    /// </param>
    public DomainNameSystemSpoofingEntryViewModel(
        DomainNameSystemOverrideEntry entry,
        DomainNameSystemSpoofingEntryEnabledChanged onIsEnabledChanged)
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
    ///     Pushes a new enabled state into the row without invoking the parent callback.
    ///     Can be used by the owning view model to mirror map-level mutations back into
    ///     the row when those mutations did not originate from binding.
    /// </summary>
    /// <param name="value">The new enabled state.</param>
    public void SetIsEnabledFromMap(bool value)
    {
        if (IsEnabled == value)
        {
            return;
        }

        _isSuppressingEnabledCallback = true;
        try
        {
            IsEnabled = value;
        }
        finally
        {
            _isSuppressingEnabledCallback = false;
        }
    }

    /// <summary>
    ///     Updates the displayed <see cref="MatchCount" />. Should be invoked on the UI
    ///     thread by the owning view model after reading the latest counter from the
    ///     map.
    /// </summary>
    /// <param name="value">The new match count to display.</param>
    public void SyncMatchCount(int value)
    {
        if (MatchCount != value)
        {
            MatchCount = value;
        }
    }

    partial void OnIsEnabledChanged(bool value)
    {
        if (_isSuppressingEnabledCallback)
        {
            return;
        }

        _onIsEnabledChanged(this, value);
    }
}
