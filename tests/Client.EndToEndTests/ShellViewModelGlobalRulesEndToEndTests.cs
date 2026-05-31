using Proxyfan.Client.EndToEndTests.Infrastructure;
using System.Threading.Tasks;
using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.Core;

namespace Proxyfan.Client.EndToEndTests;

/// <summary>
///     End-to-end UI tests covering the No-Caching and Breakpoint global toggles
///     wired into <see cref="Proxyfan.Client.Shell.ViewModels.ShellViewModel" />,
///     per <c>docs/DESIGN.md § 6.7 Breakpoints</c> and § 6.11 No Caching. Verifies
///     the toggle command flips both the underlying domain object and the
///     observable property that drives any UI binding.
/// </summary>
public sealed class ShellViewModelGlobalRulesEndToEndTests : EndToEndTestBase
{
    [Test]
    public async Task IsNoCachingEnabled_FreshShell_StartsDisabled()
    {
        await RunOnUiThreadAsync(async () =>
        {
            using var env = new TestShellEnvironment();
            await Assert.That(env.ShellViewModel.IsNoCachingEnabled).IsFalse();
            await Assert.That(env.NoCachingRule.IsEnabled).IsFalse();
        });
    }

    [Test]
    public async Task ToggleNoCaching_FromDisabled_EnablesBothVmAndDomainState()
    {
        await RunOnUiThreadAsync(async () =>
        {
            using var env = new TestShellEnvironment();

            env.ShellViewModel.ToggleNoCachingCommand.Execute(null);

            await Assert.That(env.ShellViewModel.IsNoCachingEnabled).IsTrue();
            await Assert.That(env.NoCachingRule.IsEnabled).IsTrue();
        });
    }

    [Test]
    public async Task ToggleNoCaching_Twice_ReturnsToDisabled()
    {
        await RunOnUiThreadAsync(async () =>
        {
            using var env = new TestShellEnvironment();

            env.ShellViewModel.ToggleNoCachingCommand.Execute(null);
            env.ShellViewModel.ToggleNoCachingCommand.Execute(null);

            await Assert.That(env.ShellViewModel.IsNoCachingEnabled).IsFalse();
            await Assert.That(env.NoCachingRule.IsEnabled).IsFalse();
        });
    }

    [Test]
    public async Task IsBreakpointEnabled_FreshShell_StartsDisabled()
    {
        await RunOnUiThreadAsync(async () =>
        {
            using var env = new TestShellEnvironment();
            await Assert.That(env.ShellViewModel.IsBreakpointEnabled).IsFalse();
            await Assert.That(env.BreakpointConfiguration.IsEnabled).IsFalse();
        });
    }

    [Test]
    public async Task ToggleBreakpoint_FromDisabled_EnablesBothVmAndDomainState()
    {
        await RunOnUiThreadAsync(async () =>
        {
            using var env = new TestShellEnvironment();

            env.ShellViewModel.ToggleBreakpointCommand.Execute(null);

            await Assert.That(env.ShellViewModel.IsBreakpointEnabled).IsTrue();
            await Assert.That(env.BreakpointConfiguration.IsEnabled).IsTrue();
        });
    }

    [Test]
    public async Task ToggleBreakpoint_Twice_ReturnsToDisabled()
    {
        await RunOnUiThreadAsync(async () =>
        {
            using var env = new TestShellEnvironment();

            env.ShellViewModel.ToggleBreakpointCommand.Execute(null);
            env.ShellViewModel.ToggleBreakpointCommand.Execute(null);

            await Assert.That(env.ShellViewModel.IsBreakpointEnabled).IsFalse();
            await Assert.That(env.BreakpointConfiguration.IsEnabled).IsFalse();
        });
    }
}
