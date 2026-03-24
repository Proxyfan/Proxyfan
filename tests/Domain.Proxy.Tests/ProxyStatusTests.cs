using System.Threading.Tasks;

namespace Proxyfan.Domain.Proxy.Tests;

/// <summary>Tests for <see cref="ProxyStatus" />.</summary>
internal sealed class ProxyStatusTests
{
    /// <summary>Verifies that <see cref="ProxyStatus.Stopped" /> has integer value 0.</summary>
    [Test]
    public async Task Stopped_HasValue0()
    {
        var value = (int)ProxyStatus.Stopped;
        await Assert.That(value).IsEqualTo(0);
    }

    /// <summary>Verifies that all expected enum members are defined.</summary>
    [Test]
    public async Task AllExpectedMembers_AreDefined()
    {
        await Assert.That(System.Enum.IsDefined(ProxyStatus.Stopped)).IsTrue();
        await Assert.That(System.Enum.IsDefined(ProxyStatus.Starting)).IsTrue();
        await Assert.That(System.Enum.IsDefined(ProxyStatus.Running)).IsTrue();
        await Assert.That(System.Enum.IsDefined(ProxyStatus.Stopping)).IsTrue();
        await Assert.That(System.Enum.IsDefined(ProxyStatus.Faulted)).IsTrue();
    }
}
