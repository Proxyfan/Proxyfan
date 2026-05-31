using Proxyfan.Client.EndToEndTests.Infrastructure;
using System.Threading.Tasks;
using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.Core;

namespace Proxyfan.Client.EndToEndTests;

/// <summary>
///     End-to-end UI tests covering <see cref="Proxyfan.Client.Shell.ViewModels.ShellViewModel.ToggleSystemProxyCommand" />
///     described in <c>docs/DESIGN.md § 4.6 Toolbar</c>: clicking the
///     Enable/Disable proxy toolbar button registers / unregisters the proxy
///     with the OS and flips <c>IsSystemProxyEnabled</c>.
/// </summary>
public sealed class ShellViewModelSystemProxyEndToEndTests : EndToEndTestBase
{
    [Test]
    public async Task IsSystemProxyEnabled_FreshShell_StartsDisabled()
    {
        await RunOnUiThreadAsync(async () =>
        {
            using var env = new TestShellEnvironment(port: 9999);
            await Assert.That(env.ShellViewModel.IsSystemProxyEnabled).IsFalse();
        });
    }

    [Test]
    public async Task ToggleSystemProxy_FromDisabled_RegistersConfiguredPort()
    {
        await RunOnUiThreadAsync(async () =>
        {
            using var env = new TestShellEnvironment(port: 9090);

            await env.ShellViewModel.ToggleSystemProxyCommand.ExecuteAsync(null);

            await Assert.That(env.ShellViewModel.IsSystemProxyEnabled).IsTrue();
            await Assert.That(env.SystemProxy.RegisteredPorts.Count).IsEqualTo(1);
            await Assert.That(env.SystemProxy.RegisteredPorts[0]).IsEqualTo(9090);
            await Assert.That(env.SystemProxy.UnregisterCount).IsEqualTo(0);
        });
    }

    [Test]
    public async Task ToggleSystemProxy_TwiceFromDisabled_RegistersThenUnregisters()
    {
        await RunOnUiThreadAsync(async () =>
        {
            using var env = new TestShellEnvironment(port: 8181);

            await env.ShellViewModel.ToggleSystemProxyCommand.ExecuteAsync(null);
            await env.ShellViewModel.ToggleSystemProxyCommand.ExecuteAsync(null);

            await Assert.That(env.ShellViewModel.IsSystemProxyEnabled).IsFalse();
            await Assert.That(env.SystemProxy.RegisteredPorts.Count).IsEqualTo(1);
            await Assert.That(env.SystemProxy.UnregisterCount).IsEqualTo(1);
        });
    }

    [Test]
    public async Task ToggleSystemProxy_ThreeTimes_LeavesEnabled()
    {
        await RunOnUiThreadAsync(async () =>
        {
            using var env = new TestShellEnvironment(port: 7000);

            await env.ShellViewModel.ToggleSystemProxyCommand.ExecuteAsync(null);
            await env.ShellViewModel.ToggleSystemProxyCommand.ExecuteAsync(null);
            await env.ShellViewModel.ToggleSystemProxyCommand.ExecuteAsync(null);

            await Assert.That(env.ShellViewModel.IsSystemProxyEnabled).IsTrue();
            await Assert.That(env.SystemProxy.RegisteredPorts.Count).IsEqualTo(2);
            await Assert.That(env.SystemProxy.UnregisterCount).IsEqualTo(1);
        });
    }
}
