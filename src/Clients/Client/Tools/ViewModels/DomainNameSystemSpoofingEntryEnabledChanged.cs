namespace Proxyfan.Client.Tools.ViewModels;

/// <summary>
///     Called by <see cref="DomainNameSystemSpoofingEntryViewModel" /> when the bound
///     <see cref="DomainNameSystemSpoofingEntryViewModel.IsEnabled" /> property changes.
///     The owning view model is expected to route the new value through the
///     <see cref="Proxyfan.Domain.DomainNameSystemSpoofing.DomainNameSystemOverrideMap" />
///     rather than letting the row mutate the underlying entry directly.
/// </summary>
/// <param name="row">The row whose enabled state changed.</param>
/// <param name="value">The new enabled state.</param>
public delegate void DomainNameSystemSpoofingEntryEnabledChanged(
    DomainNameSystemSpoofingEntryViewModel row,
    bool value);
