using Proxyfan.Client.Shell.ViewModels;
using Proxyfan.Client.Tests.Stubs;
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
        var viewModel = ShellViewModelFactory.Create(new StubSystemProxy(), 8080);

        await Assert.That(() => viewModel.ExitCommand.Execute(null)).ThrowsNothing();
    }
}