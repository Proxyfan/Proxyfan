using CommunityToolkit.Mvvm.ComponentModel;
using Proxyfan.Domain.DomainNameSystemSpoofing;

namespace Proxyfan.Client.Tools.ViewModels;

/// <summary>
///     View model for a single DNS override entry, exposing the host name and target
///     IP address as displayable strings backed by the domain entry.
/// </summary>
public sealed partial class DomainNameSystemSpoofingEntryViewModel : ObservableObject
{
    /// <summary>
    ///     Gets the underlying domain entry.
    /// </summary>
    public DomainNameSystemOverrideEntry Entry { get; }

    /// <summary>
    ///     Gets the host name being overridden.
    /// </summary>
    public string Hostname { get; }

    /// <summary>
    ///     Gets the override IP address as a display string.
    /// </summary>
    public string OverrideAddress { get; }

    /// <summary>
    ///     Initializes a new <see cref="DomainNameSystemSpoofingEntryViewModel" /> wrapping
    ///     the supplied entry.
    /// </summary>
    /// <param name="entry">The domain entry to expose.</param>
    public DomainNameSystemSpoofingEntryViewModel(DomainNameSystemOverrideEntry entry)
    {
        Entry = entry;
        Hostname = entry.Hostname;
        OverrideAddress = entry.OverrideAddress.ToString();
    }
}
