using System;
using System.Threading.Tasks;

namespace Proxyfan.Domain.Proxy.Tests;

/// <summary>
///     Tests for <see cref="ProxyStatus" />.
/// </summary>
public sealed class ProxyStatusTests
{
    /// <summary>
    ///     Verifies that all expected enum members are defined.
    /// </summary>
    [Test]
    public async Task AllExpectedMembers_WhenChecked_AreDefined()
    {
        await Assert.That(Enum.IsDefined(ProxyStatus.Stopped)).IsTrue();
        await Assert.That(Enum.IsDefined(ProxyStatus.Starting)).IsTrue();
        await Assert.That(Enum.IsDefined(ProxyStatus.Running)).IsTrue();
        await Assert.That(Enum.IsDefined(ProxyStatus.Stopping)).IsTrue();
        await Assert.That(Enum.IsDefined(ProxyStatus.Faulted)).IsTrue();
    }

    /// <summary>
    ///     Verifies that <see cref="ProxyStatus.Stopped" /> has integer value 0.
    /// </summary>
    [Test]
    public async Task Stopped_WhenCastToInt_HasValueZero()
    {
        var value = (int)ProxyStatus.Stopped;
        await Assert.That(value).IsEqualTo(0);
    }
}