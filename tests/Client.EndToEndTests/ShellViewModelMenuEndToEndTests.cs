using Proxyfan.Client.EndToEndTests.Infrastructure;
using System.Threading.Tasks;
using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.Core;

namespace Proxyfan.Client.EndToEndTests;

/// <summary>
///     End-to-end UI tests asserting that every Tools-menu command on the
///     <see cref="Proxyfan.Client.Shell.ViewModels.ShellViewModel" /> invokes the
///     matching <c>OpenXxx</c> on the registered <see cref="Proxyfan.Client.Tools.IToolWindowOpener" />.
///     Covers <c>docs/DESIGN.md § 4.5 Menu Bar</c> and all referenced § 6 tool windows.
/// </summary>
public sealed class ShellViewModelMenuEndToEndTests : EndToEndTestBase
{
    [Test]
    public async Task OpenBlockListCommand_Invoked_RaisesOpenBlockListExactlyOnce()
    {
        await RunOnUiThreadAsync(async () =>
        {
            using var env = new TestShellEnvironment();

            env.ShellViewModel.OpenBlockListCommand.Execute(null);

            await Assert.That(env.ToolWindowOpener.OpenBlockListCallCount).IsEqualTo(1);
        });
    }

    [Test]
    public async Task OpenAllowListCommand_Invoked_RaisesOpenAllowListExactlyOnce()
    {
        await RunOnUiThreadAsync(async () =>
        {
            using var env = new TestShellEnvironment();

            env.ShellViewModel.OpenAllowListCommand.Execute(null);

            await Assert.That(env.ToolWindowOpener.OpenAllowListCallCount).IsEqualTo(1);
        });
    }

    [Test]
    public async Task OpenBreakpointCommand_Invoked_RaisesOpenBreakpointExactlyOnce()
    {
        await RunOnUiThreadAsync(async () =>
        {
            using var env = new TestShellEnvironment();

            env.ShellViewModel.OpenBreakpointCommand.Execute(null);

            await Assert.That(env.ToolWindowOpener.OpenBreakpointCallCount).IsEqualTo(1);
        });
    }

    [Test]
    public async Task OpenCertificateManagerCommand_Invoked_RaisesOpenCertificateManagerExactlyOnce()
    {
        await RunOnUiThreadAsync(async () =>
        {
            using var env = new TestShellEnvironment();

            env.ShellViewModel.OpenCertificateManagerCommand.Execute(null);

            await Assert.That(env.ToolWindowOpener.OpenCertificateManagerCallCount).IsEqualTo(1);
        });
    }

    [Test]
    public async Task OpenComposerCommand_Invoked_RaisesOpenComposerExactlyOnce()
    {
        await RunOnUiThreadAsync(async () =>
        {
            using var env = new TestShellEnvironment();

            env.ShellViewModel.OpenComposerCommand.Execute(null);

            await Assert.That(env.ToolWindowOpener.OpenComposerCallCount).IsEqualTo(1);
        });
    }

    [Test]
    public async Task OpenScriptingCommand_Invoked_RaisesOpenScriptingExactlyOnce()
    {
        await RunOnUiThreadAsync(async () =>
        {
            using var env = new TestShellEnvironment();

            env.ShellViewModel.OpenScriptingCommand.Execute(null);

            await Assert.That(env.ToolWindowOpener.OpenScriptingCallCount).IsEqualTo(1);
        });
    }

    [Test]
    public async Task OpenDiffToolCommand_Invoked_RaisesOpenDiffToolExactlyOnce()
    {
        await RunOnUiThreadAsync(async () =>
        {
            using var env = new TestShellEnvironment();

            env.ShellViewModel.OpenDiffToolCommand.Execute(null);

            await Assert.That(env.ToolWindowOpener.OpenDiffToolCallCount).IsEqualTo(1);
        });
    }

    [Test]
    public async Task OpenCustomColumnsCommand_Invoked_RaisesOpenCustomColumnsExactlyOnce()
    {
        await RunOnUiThreadAsync(async () =>
        {
            using var env = new TestShellEnvironment();

            env.ShellViewModel.OpenCustomColumnsCommand.Execute(null);

            await Assert.That(env.ToolWindowOpener.OpenCustomColumnsCallCount).IsEqualTo(1);
        });
    }

    [Test]
    public async Task OpenMapLocalCommand_Invoked_RaisesOpenMapLocalExactlyOnce()
    {
        await RunOnUiThreadAsync(async () =>
        {
            using var env = new TestShellEnvironment();

            env.ShellViewModel.OpenMapLocalCommand.Execute(null);

            await Assert.That(env.ToolWindowOpener.OpenMapLocalCallCount).IsEqualTo(1);
        });
    }

    [Test]
    public async Task OpenMapRemoteCommand_Invoked_RaisesOpenMapRemoteExactlyOnce()
    {
        await RunOnUiThreadAsync(async () =>
        {
            using var env = new TestShellEnvironment();

            env.ShellViewModel.OpenMapRemoteCommand.Execute(null);

            await Assert.That(env.ToolWindowOpener.OpenMapRemoteCallCount).IsEqualTo(1);
        });
    }

    [Test]
    public async Task OpenSecureSocketsLayerProxyingCommand_Invoked_RaisesOpenSecureSocketsLayerProxyingExactlyOnce()
    {
        await RunOnUiThreadAsync(async () =>
        {
            using var env = new TestShellEnvironment();

            env.ShellViewModel.OpenSecureSocketsLayerProxyingCommand.Execute(null);

            await Assert.That(env.ToolWindowOpener.OpenSecureSocketsLayerProxyingCallCount).IsEqualTo(1);
        });
    }

    [Test]
    public async Task OpenDomainNameSystemSpoofingCommand_Invoked_RaisesOpenDomainNameSystemSpoofingExactlyOnce()
    {
        await RunOnUiThreadAsync(async () =>
        {
            using var env = new TestShellEnvironment();

            env.ShellViewModel.OpenDomainNameSystemSpoofingCommand.Execute(null);

            await Assert.That(env.ToolWindowOpener.OpenDomainNameSystemSpoofingCallCount).IsEqualTo(1);
        });
    }

    [Test]
    public async Task OpenReverseProxyCommand_Invoked_RaisesOpenReverseProxyExactlyOnce()
    {
        await RunOnUiThreadAsync(async () =>
        {
            using var env = new TestShellEnvironment();

            env.ShellViewModel.OpenReverseProxyCommand.Execute(null);

            await Assert.That(env.ToolWindowOpener.OpenReverseProxyCallCount).IsEqualTo(1);
        });
    }

    [Test]
    public async Task OpenRemoteDevicesCommand_Invoked_RaisesOpenRemoteDevicesExactlyOnce()
    {
        await RunOnUiThreadAsync(async () =>
        {
            using var env = new TestShellEnvironment();

            env.ShellViewModel.OpenRemoteDevicesCommand.Execute(null);

            await Assert.That(env.ToolWindowOpener.OpenRemoteDevicesCallCount).IsEqualTo(1);
        });
    }

    [Test]
    public async Task OpenRemoteProcedureCallDescriptorsCommand_Invoked_RaisesOpenRemoteProcedureCallDescriptorsExactlyOnce()
    {
        await RunOnUiThreadAsync(async () =>
        {
            using var env = new TestShellEnvironment();

            env.ShellViewModel.OpenRemoteProcedureCallDescriptorsCommand.Execute(null);

            await Assert.That(env.ToolWindowOpener.OpenRemoteProcedureCallDescriptorsCallCount).IsEqualTo(1);
        });
    }

    [Test]
    public async Task OpenThrottleCommand_Invoked_RaisesOpenThrottleExactlyOnce()
    {
        await RunOnUiThreadAsync(async () =>
        {
            using var env = new TestShellEnvironment();

            env.ShellViewModel.OpenThrottleCommand.Execute(null);

            await Assert.That(env.ToolWindowOpener.OpenThrottleCallCount).IsEqualTo(1);
        });
    }

    [Test]
    public async Task OpenThemeCommand_Invoked_RaisesOpenThemeExactlyOnce()
    {
        await RunOnUiThreadAsync(async () =>
        {
            using var env = new TestShellEnvironment();

            env.ShellViewModel.OpenThemeCommand.Execute(null);

            await Assert.That(env.ToolWindowOpener.OpenThemeCallCount).IsEqualTo(1);
        });
    }

    [Test]
    public async Task OpenKeyboardShortcutsCommand_Invoked_RaisesOpenKeyboardShortcutsExactlyOnce()
    {
        await RunOnUiThreadAsync(async () =>
        {
            using var env = new TestShellEnvironment();

            env.ShellViewModel.OpenKeyboardShortcutsCommand.Execute(null);

            await Assert.That(env.ToolWindowOpener.OpenKeyboardShortcutsCallCount).IsEqualTo(1);
        });
    }

    [Test]
    public async Task OpenPluginManagerCommand_Invoked_RaisesOpenPluginManagerExactlyOnce()
    {
        await RunOnUiThreadAsync(async () =>
        {
            using var env = new TestShellEnvironment();

            env.ShellViewModel.OpenPluginManagerCommand.Execute(null);

            await Assert.That(env.ToolWindowOpener.OpenPluginManagerCallCount).IsEqualTo(1);
        });
    }

    [Test]
    public async Task OpenPreferencesCommand_Invoked_RaisesOpenPreferencesExactlyOnce()
    {
        await RunOnUiThreadAsync(async () =>
        {
            using var env = new TestShellEnvironment();

            env.ShellViewModel.OpenPreferencesCommand.Execute(null);

            await Assert.That(env.ToolWindowOpener.OpenPreferencesCallCount).IsEqualTo(1);
        });
    }
}
