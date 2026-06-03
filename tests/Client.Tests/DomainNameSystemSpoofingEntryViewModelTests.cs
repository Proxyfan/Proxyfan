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

        var viewModel = new DomainNameSystemSpoofingEntryViewModel(entry, (_, _) => { });

        await Assert.That(viewModel.CanonicalPattern).IsEqualTo(entry.CanonicalPattern);
        await Assert.That(viewModel.Hostname).IsEqualTo("example.test");
        await Assert.That(viewModel.OverrideAddress).IsEqualTo("10.0.0.1");
    }

    [Test]
    public async Task Constructor_FromIPv6Entry_FormatsAddress()
    {
        var entry = new DomainNameSystemOverrideEntry("ipv6.example", IPAddress.IPv6Loopback);

        var viewModel = new DomainNameSystemSpoofingEntryViewModel(entry, (_, _) => { });

        await Assert.That(viewModel.OverrideAddress).IsEqualTo("::1");
    }

    /// <summary>
    ///     Verifies that exact entries display the "Exact" kind label.
    /// </summary>
    [Test]
    public async Task Constructor_FromExactEntry_KindDisplayIsExact()
    {
        var entry = new DomainNameSystemOverrideEntry("api.example.com", IPAddress.Loopback);

        var viewModel = new DomainNameSystemSpoofingEntryViewModel(entry, (_, _) => { });

        await Assert.That(viewModel.KindDisplay).IsEqualTo("Exact");
    }

    /// <summary>
    ///     Verifies that wildcard entries display the "Wildcard" kind label.
    /// </summary>
    [Test]
    public async Task Constructor_FromWildcardEntry_KindDisplayIsWildcard()
    {
        var entry = new DomainNameSystemOverrideEntry("*.example.com", IPAddress.Loopback);

        var viewModel = new DomainNameSystemSpoofingEntryViewModel(entry, (_, _) => { });

        await Assert.That(viewModel.KindDisplay).IsEqualTo("Wildcard");
    }

    /// <summary>
    ///     Verifies that toggling IsEnabled invokes the parent callback rather than
    ///     mutating the underlying domain entry directly.
    /// </summary>
    [Test]
    public async Task IsEnabled_SetFalse_InvokesCallbackWithoutTouchingEntry()
    {
        var entry = new DomainNameSystemOverrideEntry("api.example.com", IPAddress.Loopback);
        DomainNameSystemSpoofingEntryViewModel? callbackRow = null;
        var callbackValue = true;
        var viewModel = new DomainNameSystemSpoofingEntryViewModel(entry, (row, value) =>
        {
            callbackRow = row;
            callbackValue = value;
        });

        viewModel.IsEnabled = false;

        await Assert.That(callbackRow).IsNotNull();
        await Assert.That(callbackValue).IsFalse();
        await Assert.That(entry.IsEnabled).IsTrue();
    }

    /// <summary>
    ///     Verifies that pushing state from the map via <see cref="DomainNameSystemSpoofingEntryViewModel.SetIsEnabledFromMap" />
    ///     updates the bindable property without re-entering the parent callback.
    /// </summary>
    [Test]
    public async Task SetIsEnabledFromMap_SetFalse_UpdatesPropertyWithoutCallback()
    {
        var entry = new DomainNameSystemOverrideEntry("api.example.com", IPAddress.Loopback);
        var callbackCount = 0;
        var viewModel = new DomainNameSystemSpoofingEntryViewModel(entry, (_, _) => callbackCount += 1);

        viewModel.SetIsEnabledFromMap(false);

        await Assert.That(viewModel.IsEnabled).IsFalse();
        await Assert.That(callbackCount).IsEqualTo(0);
    }

    /// <summary>
    ///     Verifies <see cref="DomainNameSystemSpoofingEntryViewModel.SyncMatchCount" />
    ///     updates the displayed counter.
    /// </summary>
    [Test]
    public async Task SyncMatchCount_WithNewValue_UpdatesMatchCount()
    {
        var entry = new DomainNameSystemOverrideEntry("api.example.com", IPAddress.Loopback);
        var viewModel = new DomainNameSystemSpoofingEntryViewModel(entry, (_, _) => { });

        viewModel.SyncMatchCount(7);

        await Assert.That(viewModel.MatchCount).IsEqualTo(7);
    }
}
