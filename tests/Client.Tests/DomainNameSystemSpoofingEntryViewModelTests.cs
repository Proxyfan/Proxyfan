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
}
