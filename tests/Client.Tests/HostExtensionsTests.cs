using Microsoft.Extensions.Hosting;
using Proxyfan.Client;
using System.Threading;
using System.Threading.Tasks;

namespace Proxyfan.Client.Tests;

/// <summary>
///     Tests for <see cref="HostExtensions" />.
/// </summary>
public sealed class HostExtensionsTests
{
    [Test]
    public async Task Stop_RunningHost_StopsHostSynchronously()
    {
        var builder = Host.CreateDefaultBuilder();
        var host = builder.Build();
        await host.StartAsync(CancellationToken.None);

        host.Stop();

        await Assert.That(host).IsNotNull();
        host.Dispose();
    }
}
