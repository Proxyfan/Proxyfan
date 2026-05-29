using Proxyfan.Client.Tools.ViewModels;
using Proxyfan.Domain.DomainNameSystemSpoofing;
using System.Net;
using System.Threading.Tasks;
using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.Core;

namespace Proxyfan.Client.Tests;

/// <summary>
///     Tests for <see cref="DomainNameSystemSpoofingEntryViewModel" />.
/// </summary>
public sealed class DomainNameSystemSpoofingEntryViewModelTests
{
    [Test]
    public async Task Constructor_FromEntry_FormatsHostnameAndAddress()
    {
        var entry = new DomainNameSystemOverrideEntry("example.test", IPAddress.Parse("10.0.0.1"));

        var viewModel = new DomainNameSystemSpoofingEntryViewModel(entry);

        await Assert.That(viewModel.Entry).IsSameReferenceAs(entry);
        await Assert.That(viewModel.Hostname).IsEqualTo("example.test");
        await Assert.That(viewModel.OverrideAddress).IsEqualTo("10.0.0.1");
    }

    [Test]
    public async Task Constructor_FromIPv6Entry_FormatsAddress()
    {
        var entry = new DomainNameSystemOverrideEntry("ipv6.example", IPAddress.IPv6Loopback);

        var viewModel = new DomainNameSystemSpoofingEntryViewModel(entry);

        await Assert.That(viewModel.OverrideAddress).IsEqualTo("::1");
    }

    /// <summary>
    ///     Verifies that exact entries display the "Exact" kind label.
    /// </summary>
    [Test]
    public async Task Constructor_FromExactEntry_KindDisplayIsExact()
    {
        var entry = new DomainNameSystemOverrideEntry("api.example.com", IPAddress.Loopback);

        var viewModel = new DomainNameSystemSpoofingEntryViewModel(entry);

        await Assert.That(viewModel.KindDisplay).IsEqualTo("Exact");
    }

    /// <summary>
    ///     Verifies that wildcard entries display the "Wildcard" kind label.
    /// </summary>
    [Test]
    public async Task Constructor_FromWildcardEntry_KindDisplayIsWildcard()
    {
        var entry = new DomainNameSystemOverrideEntry("*.example.com", IPAddress.Loopback);

        var viewModel = new DomainNameSystemSpoofingEntryViewModel(entry);

        await Assert.That(viewModel.KindDisplay).IsEqualTo("Wildcard");
    }

    /// <summary>
    ///     Verifies that toggling IsEnabled on the view model writes through to the underlying entry.
    /// </summary>
    [Test]
    public async Task IsEnabled_SetFalse_WritesThroughToEntry()
    {
        var entry = new DomainNameSystemOverrideEntry("api.example.com", IPAddress.Loopback);
        var viewModel = new DomainNameSystemSpoofingEntryViewModel(entry);

        viewModel.IsEnabled = false;

        await Assert.That(entry.IsEnabled).IsFalse();
    }

    /// <summary>
    ///     Verifies RefreshMatchCount pulls the latest counter value from the entry.
    /// </summary>
    [Test]
    public async Task RefreshMatchCount_AfterEntryRecordsMatches_SurfacesUpdatedCount()
    {
        var entry = new DomainNameSystemOverrideEntry("api.example.com", IPAddress.Loopback);
        var viewModel = new DomainNameSystemSpoofingEntryViewModel(entry);
        entry.RecordMatch();
        entry.RecordMatch();
        entry.RecordMatch();

        viewModel.RefreshMatchCount();

        await Assert.That(viewModel.MatchCount).IsEqualTo(3);
    }
}
