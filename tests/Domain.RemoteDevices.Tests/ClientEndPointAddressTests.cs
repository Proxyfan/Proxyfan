using System.Linq;
using System.Threading.Tasks;
using Proxyfan.Domain.RemoteDevices;
using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.Core;

namespace Proxyfan.Domain.RemoteDevices.Tests;

public sealed class ClientEndPointAddressTests
{
    [Test]
    public async Task Extract_EmptyString_ReturnsEmpty()
    {
        await Assert.That(ClientEndPointAddress.Extract(string.Empty)).IsEqualTo(string.Empty);
    }

    [Test]
    public async Task Extract_IPv4WithPort_ReturnsHost()
    {
        await Assert.That(ClientEndPointAddress.Extract("10.0.0.1:54321")).IsEqualTo("10.0.0.1");
    }

    [Test]
    public async Task Extract_IPv4WithoutPort_ReturnsInput()
    {
        await Assert.That(ClientEndPointAddress.Extract("10.0.0.1")).IsEqualTo("10.0.0.1");
    }

    [Test]
    public async Task Extract_HostnameWithPort_ReturnsHost()
    {
        await Assert.That(ClientEndPointAddress.Extract("example.com:443")).IsEqualTo("example.com");
    }

    [Test]
    public async Task Extract_HostnameWithoutPort_ReturnsInput()
    {
        await Assert.That(ClientEndPointAddress.Extract("example.com")).IsEqualTo("example.com");
    }

    [Test]
    public async Task Extract_BracketedIPv6WithPort_ReturnsHostWithoutBrackets()
    {
        await Assert.That(ClientEndPointAddress.Extract("[::1]:54321")).IsEqualTo("::1");
    }

    [Test]
    public async Task Extract_BracketedIPv6FullAddressWithPort_ReturnsHostWithoutBrackets()
    {
        await Assert.That(ClientEndPointAddress.Extract("[2001:db8::1]:443")).IsEqualTo("2001:db8::1");
    }

    [Test]
    public async Task Extract_BareIPv6Loopback_ReturnsInput()
    {
        await Assert.That(ClientEndPointAddress.Extract("::1")).IsEqualTo("::1");
    }

    [Test]
    public async Task Extract_BareIPv6Address_ReturnsInput()
    {
        await Assert.That(ClientEndPointAddress.Extract("2001:db8::1")).IsEqualTo("2001:db8::1");
    }

    [Test]
    public async Task Assembly_WhenLoaded_DoesNotReferenceDomainTraffic()
    {
        var referencedAssemblyNames = typeof(ClientEndPointAddress).Assembly
            .GetReferencedAssemblies()
            .Select(static assemblyName => assemblyName.Name)
            .ToArray();

        await Assert.That(referencedAssemblyNames).DoesNotContain("Domain.Traffic");
    }
}
