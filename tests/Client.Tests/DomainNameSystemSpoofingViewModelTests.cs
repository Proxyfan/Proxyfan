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
}
