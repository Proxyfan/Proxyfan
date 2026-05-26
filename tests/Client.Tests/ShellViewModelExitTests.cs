using Proxyfan.Client.Shell.ViewModels;
using Proxyfan.Client.Tests.Stubs;
using Proxyfan.Domain.Proxy;
using System.Threading.Tasks;

namespace Proxyfan.Client.Tests;

/// <summary>
///     Additional tests for <see cref="ShellViewModel" /> covering Exit command branches.
/// </summary>
public sealed class ShellViewModelExitTests
{
    /// <summary>
    ///     Verifies that <see cref="ShellViewModel.ExitCommand" /> can be invoked without an
    ///     Avalonia application running and does not throw.
    /// </summary>
    [Test]
    public async Task ExitCommand_WhenInvokedWithoutApplication_DoesNotThrow()
    {
        var systemProxy = new StubSystemProxy();
        var options = new ProxyOptions { Port = 8080 };
        var optionsMonitor = new StubOptionsMonitor<ProxyOptions>(options);
        var viewModel = new ShellViewModel(systemProxy, optionsMonitor);

        await Assert.That(() => viewModel.ExitCommand.Execute(null)).ThrowsNothing();
    }
}