namespace Proxyfan.Client.Tools.ViewModels;

/// <summary>
///     Callback invoked on the UI thread whenever a
///     <see cref="DomainNameSystemSpoofingEntryViewModel" />'s bindable
///     <see cref="DomainNameSystemSpoofingEntryViewModel.IsEnabled" /> changes.
///     The parent view model wires this to a map-level mutation method so that all
///     enable/disable writes flow through a single update path.
/// </summary>
/// <param name="isEnabled">The new enabled state.</param>
public delegate void DomainNameSystemSpoofingEntryIsEnabledChanged(bool isEnabled);
