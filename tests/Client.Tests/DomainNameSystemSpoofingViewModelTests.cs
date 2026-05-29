using Proxyfan.Client.Tools.ViewModels;
using Proxyfan.Domain.DomainNameSystemSpoofing;
using System.Net;
using System.Threading.Tasks;

namespace Proxyfan.Client.Tests;

/// <summary>
///     Behavioral tests for <see cref="DomainNameSystemSpoofingViewModel" />.
/// </summary>
public sealed class DomainNameSystemSpoofingViewModelTests
{
    [Test]
    public async Task Construct_WithEmptyMap_HasNoEntries()
    {
        var map = new DomainNameSystemOverrideMap();
        var viewModel = new DomainNameSystemSpoofingViewModel(map);

        await Assert.That(viewModel.Entries).IsEmpty();
        await Assert.That(viewModel.NewHostname).IsEqualTo(string.Empty);
        await Assert.That(viewModel.NewOverrideAddress).IsEqualTo(string.Empty);
        await Assert.That(viewModel.ValidationMessage).IsNull();
    }

    [Test]
    public async Task AddEntry_WithValidHostnameAndAddress_AddsToMapAndEntries()
    {
        var map = new DomainNameSystemOverrideMap();
        var viewModel = new DomainNameSystemSpoofingViewModel(map);
        viewModel.NewHostname = "api.example.com";
        viewModel.NewOverrideAddress = "127.0.0.1";

        viewModel.AddEntryCommand.Execute(null);

        await Assert.That(viewModel.Entries).HasCount(1);
        await Assert.That(viewModel.Entries[0].Hostname).IsEqualTo("api.example.com");
        await Assert.That(viewModel.Entries[0].OverrideAddress).IsEqualTo("127.0.0.1");
        await Assert.That(map.HasOverride("api.example.com")).IsTrue();
        await Assert.That(map.Resolve("api.example.com")).IsEqualTo(IPAddress.Loopback);
        await Assert.That(viewModel.NewHostname).IsEqualTo(string.Empty);
        await Assert.That(viewModel.NewOverrideAddress).IsEqualTo(string.Empty);
        await Assert.That(viewModel.ValidationMessage).IsNull();
    }

    [Test]
    public async Task AddEntry_WithEmptyHostname_SetsValidationMessage()
    {
        var map = new DomainNameSystemOverrideMap();
        var viewModel = new DomainNameSystemSpoofingViewModel(map);
        viewModel.NewHostname = "   ";
        viewModel.NewOverrideAddress = "127.0.0.1";

        viewModel.AddEntryCommand.Execute(null);

        await Assert.That(viewModel.Entries).IsEmpty();
        await Assert.That(viewModel.ValidationMessage).IsNotNull();
    }

    [Test]
    public async Task AddEntry_WithInvalidAddress_SetsValidationMessage()
    {
        var map = new DomainNameSystemOverrideMap();
        var viewModel = new DomainNameSystemSpoofingViewModel(map);
        viewModel.NewHostname = "api.example.com";
        viewModel.NewOverrideAddress = "not-an-ip";

        viewModel.AddEntryCommand.Execute(null);

        await Assert.That(viewModel.Entries).IsEmpty();
        await Assert.That(viewModel.ValidationMessage).IsNotNull();
        await Assert.That(map.HasOverride("api.example.com")).IsFalse();
    }

    [Test]
    public async Task AddEntry_WithDuplicateHostname_SetsValidationMessage()
    {
        var map = new DomainNameSystemOverrideMap();
        var viewModel = new DomainNameSystemSpoofingViewModel(map);
        viewModel.NewHostname = "api.example.com";
        viewModel.NewOverrideAddress = "127.0.0.1";
        viewModel.AddEntryCommand.Execute(null);

        viewModel.NewHostname = "api.example.com";
        viewModel.NewOverrideAddress = "10.0.0.1";
        viewModel.AddEntryCommand.Execute(null);

        await Assert.That(viewModel.Entries).HasCount(1);
        await Assert.That(viewModel.ValidationMessage).IsNotNull();
    }

    [Test]
    public async Task RemoveEntry_WithExistingEntry_RemovesFromMapAndEntries()
    {
        var map = new DomainNameSystemOverrideMap();
        var viewModel = new DomainNameSystemSpoofingViewModel(map);
        viewModel.NewHostname = "api.example.com";
        viewModel.NewOverrideAddress = "127.0.0.1";
        viewModel.AddEntryCommand.Execute(null);
        var entry = viewModel.Entries[0];

        viewModel.RemoveEntryCommand.Execute(entry);

        await Assert.That(viewModel.Entries).IsEmpty();
        await Assert.That(map.HasOverride("api.example.com")).IsFalse();
    }

    [Test]
    public async Task RemoveEntry_WithNull_NoOp()
    {
        var map = new DomainNameSystemOverrideMap();
        var viewModel = new DomainNameSystemSpoofingViewModel(map);
        viewModel.NewHostname = "api.example.com";
        viewModel.NewOverrideAddress = "127.0.0.1";
        viewModel.AddEntryCommand.Execute(null);

        viewModel.RemoveEntryCommand.Execute(null);

        await Assert.That(viewModel.Entries).HasCount(1);
    }

    /// <summary>
    ///     Verifies pre-existing map entries are surfaced when the VM is constructed.
    /// </summary>
    [Test]
    public async Task Construct_WithPrePopulatedMap_SurfacesExistingEntries()
    {
        var map = new DomainNameSystemOverrideMap();
        map.Add(new DomainNameSystemOverrideEntry("api.example.com", IPAddress.Loopback));
        map.Add(new DomainNameSystemOverrideEntry("*.example.org", IPAddress.Parse("10.0.0.1")));

        var viewModel = new DomainNameSystemSpoofingViewModel(map);

        await Assert.That(viewModel.Entries).HasCount(2);
        await Assert.That(viewModel.Entries[0].Hostname).IsEqualTo("api.example.com");
        await Assert.That(viewModel.Entries[1].Hostname).IsEqualTo("*.example.org");
    }

    /// <summary>
    ///     Verifies the IsActive default mirrors the underlying map.
    /// </summary>
    [Test]
    public async Task Construct_OnNewMap_DefaultsToActive()
    {
        var map = new DomainNameSystemOverrideMap();

        var viewModel = new DomainNameSystemSpoofingViewModel(map);

        await Assert.That(viewModel.IsActive).IsTrue();
        await Assert.That(viewModel.StatusDisplay).IsEqualTo("Active — spoofing 0 of 0 domains");
    }

    /// <summary>
    ///     Verifies toggling IsActive on the view model writes through to the map and updates status.
    /// </summary>
    [Test]
    public async Task IsActive_SetFalse_WritesThroughToMapAndUpdatesStatus()
    {
        var map = new DomainNameSystemOverrideMap();
        var viewModel = new DomainNameSystemSpoofingViewModel(map);

        viewModel.IsActive = false;

        await Assert.That(map.IsActive).IsFalse();
        await Assert.That(viewModel.StatusDisplay).IsEqualTo("Inactive");
    }

    /// <summary>
    ///     Verifies that adding a wildcard pattern is accepted.
    /// </summary>
    [Test]
    public async Task AddEntry_WithWildcardPattern_Accepted()
    {
        var map = new DomainNameSystemOverrideMap();
        var viewModel = new DomainNameSystemSpoofingViewModel(map);
        viewModel.NewHostname = "*.example.com";
        viewModel.NewOverrideAddress = "127.0.0.1";

        viewModel.AddEntryCommand.Execute(null);

        await Assert.That(viewModel.Entries).HasCount(1);
        await Assert.That(viewModel.Entries[0].KindDisplay).IsEqualTo("Wildcard");
        await Assert.That(map.HasOverride("*.example.com")).IsTrue();
    }

    /// <summary>
    ///     Verifies an invalid pattern (embedded colon for a port) is rejected with a message.
    /// </summary>
    [Test]
    public async Task AddEntry_WithInvalidPattern_SetsValidationMessage()
    {
        var map = new DomainNameSystemOverrideMap();
        var viewModel = new DomainNameSystemSpoofingViewModel(map);
        viewModel.NewHostname = "example.com:8080";
        viewModel.NewOverrideAddress = "127.0.0.1";

        viewModel.AddEntryCommand.Execute(null);

        await Assert.That(viewModel.Entries).IsEmpty();
        await Assert.That(viewModel.ValidationMessage).IsNotNull();
    }

    /// <summary>
    ///     Verifies EnableAllEntries flips every entry to enabled.
    /// </summary>
    [Test]
    public async Task EnableAllEntries_AfterDisabling_EnablesAll()
    {
        var map = new DomainNameSystemOverrideMap();
        map.Add(new DomainNameSystemOverrideEntry("api.example.com", IPAddress.Loopback));
        map.Add(new DomainNameSystemOverrideEntry("*.example.org", IPAddress.Loopback));
        var viewModel = new DomainNameSystemSpoofingViewModel(map);
        viewModel.Entries[0].IsEnabled = false;
        viewModel.Entries[1].IsEnabled = false;

        viewModel.EnableAllEntriesCommand.Execute(null);

        await Assert.That(viewModel.Entries[0].IsEnabled).IsTrue();
        await Assert.That(viewModel.Entries[1].IsEnabled).IsTrue();
        await Assert.That(map.GetSnapshot()[0].IsEnabled).IsTrue();
        await Assert.That(map.GetSnapshot()[1].IsEnabled).IsTrue();
    }

    /// <summary>
    ///     Verifies DisableAllEntries flips every entry to disabled.
    /// </summary>
    [Test]
    public async Task DisableAllEntries_FromEnabled_DisablesAll()
    {
        var map = new DomainNameSystemOverrideMap();
        map.Add(new DomainNameSystemOverrideEntry("api.example.com", IPAddress.Loopback));
        map.Add(new DomainNameSystemOverrideEntry("*.example.org", IPAddress.Loopback));
        var viewModel = new DomainNameSystemSpoofingViewModel(map);

        viewModel.DisableAllEntriesCommand.Execute(null);

        await Assert.That(viewModel.Entries[0].IsEnabled).IsFalse();
        await Assert.That(viewModel.Entries[1].IsEnabled).IsFalse();
    }

    /// <summary>
    ///     Verifies RefreshMatchCounts pulls each entry's counter into its VM.
    /// </summary>
    [Test]
    public async Task RefreshMatchCounts_AfterResolvingTraffic_UpdatesViewModelCounters()
    {
        var map = new DomainNameSystemOverrideMap();
        map.Add(new DomainNameSystemOverrideEntry("api.example.com", IPAddress.Loopback));
        var viewModel = new DomainNameSystemSpoofingViewModel(map);
        map.Resolve("api.example.com");
        map.Resolve("api.example.com");

        viewModel.RefreshMatchCountsCommand.Execute(null);

        await Assert.That(viewModel.Entries[0].MatchCount).IsEqualTo(2);
    }

    /// <summary>
    ///     Verifies ResetMatchCounts zeroes both the underlying counter and the VM display.
    /// </summary>
    [Test]
    public async Task ResetMatchCounts_AfterRecordingMatches_ZeroesEntriesAndViewModel()
    {
        var map = new DomainNameSystemOverrideMap();
        var entry = new DomainNameSystemOverrideEntry("api.example.com", IPAddress.Loopback);
        map.Add(entry);
        var viewModel = new DomainNameSystemSpoofingViewModel(map);
        entry.RecordMatch();
        entry.RecordMatch();

        viewModel.ResetMatchCountsCommand.Execute(null);

        await Assert.That(entry.MatchCount).IsEqualTo(0);
        await Assert.That(viewModel.Entries[0].MatchCount).IsEqualTo(0);
    }

    /// <summary>
    ///     Verifies the status string reflects the number of enabled entries.
    /// </summary>
    [Test]
    public async Task StatusDisplay_AfterDisablingOneEntry_ReflectsEnabledCount()
    {
        var map = new DomainNameSystemOverrideMap();
        map.Add(new DomainNameSystemOverrideEntry("api.example.com", IPAddress.Loopback));
        map.Add(new DomainNameSystemOverrideEntry("*.example.org", IPAddress.Loopback));
        var viewModel = new DomainNameSystemSpoofingViewModel(map);

        viewModel.Entries[0].IsEnabled = false;

        await Assert.That(viewModel.StatusDisplay).IsEqualTo("Active — spoofing 1 of 2 domains");
    }
}
