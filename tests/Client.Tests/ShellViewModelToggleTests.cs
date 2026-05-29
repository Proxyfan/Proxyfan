using Proxyfan.Client.Tests.Stubs;
using Proxyfan.Domain.Rules.Rules;
using Proxyfan.Domain.Updates;
using System.Threading.Tasks;

namespace Proxyfan.Client.Tests;

/// <summary>
///     Tests for the No-Caching and Breakpoint toggle commands on <see cref="Client.Shell.ViewModels.ShellViewModel" />.
/// </summary>
public sealed class ShellViewModelToggleTests
{
    /// <summary>
    ///     Toggling no-caching from a disabled rule enables both the rule and the observable property.
    /// </summary>
    [Test]
    public async Task ToggleNoCaching_FromDisabled_EnablesRuleAndProperty()
    {
        var noCachingRule = new MutableNoCachingRule(priority: 400, isEnabled: false);
        var breakpointConfiguration = new MutableBreakpointConfiguration(isEnabled: false);
        var systemProxy = new StubSystemProxy();
        var shell = ShellViewModelFactory.Create(
            systemProxy,
            8080,
            new ShellViewModelFactory.StubFilePickerService(),
            new ShellViewModelFactory.StubHarExporter(),
            new ShellViewModelFactory.StubHarImporter(),
            new StubToolWindowOpener(),
            new MutableUpdateNotification(),
            noCachingRule,
            breakpointConfiguration);

        shell.ToggleNoCachingCommand.Execute(null);

        await Assert.That(noCachingRule.IsEnabled).IsTrue();
        await Assert.That(shell.IsNoCachingEnabled).IsTrue();
    }

    /// <summary>
    ///     Toggling no-caching from an enabled rule disables both the rule and the observable property.
    /// </summary>
    [Test]
    public async Task ToggleNoCaching_FromEnabled_DisablesRuleAndProperty()
    {
        var noCachingRule = new MutableNoCachingRule(priority: 400, isEnabled: true);
        var breakpointConfiguration = new MutableBreakpointConfiguration(isEnabled: false);
        var systemProxy = new StubSystemProxy();
        var shell = ShellViewModelFactory.Create(
            systemProxy,
            8080,
            new ShellViewModelFactory.StubFilePickerService(),
            new ShellViewModelFactory.StubHarExporter(),
            new ShellViewModelFactory.StubHarImporter(),
            new StubToolWindowOpener(),
            new MutableUpdateNotification(),
            noCachingRule,
            breakpointConfiguration);

        shell.ToggleNoCachingCommand.Execute(null);

        await Assert.That(noCachingRule.IsEnabled).IsFalse();
        await Assert.That(shell.IsNoCachingEnabled).IsFalse();
    }

    /// <summary>
    ///     Toggling breakpoint from a disabled configuration enables both the configuration and the observable property.
    /// </summary>
    [Test]
    public async Task ToggleBreakpoint_FromDisabled_EnablesConfigurationAndProperty()
    {
        var noCachingRule = new MutableNoCachingRule(priority: 400, isEnabled: false);
        var breakpointConfiguration = new MutableBreakpointConfiguration(isEnabled: false);
        var systemProxy = new StubSystemProxy();
        var shell = ShellViewModelFactory.Create(
            systemProxy,
            8080,
            new ShellViewModelFactory.StubFilePickerService(),
            new ShellViewModelFactory.StubHarExporter(),
            new ShellViewModelFactory.StubHarImporter(),
            new StubToolWindowOpener(),
            new MutableUpdateNotification(),
            noCachingRule,
            breakpointConfiguration);

        shell.ToggleBreakpointCommand.Execute(null);

        await Assert.That(breakpointConfiguration.IsEnabled).IsTrue();
        await Assert.That(shell.IsBreakpointEnabled).IsTrue();
    }

    /// <summary>
    ///     Toggling breakpoint from an enabled configuration disables both the configuration and the observable property.
    /// </summary>
    [Test]
    public async Task ToggleBreakpoint_FromEnabled_DisablesConfigurationAndProperty()
    {
        var noCachingRule = new MutableNoCachingRule(priority: 400, isEnabled: false);
        var breakpointConfiguration = new MutableBreakpointConfiguration(isEnabled: true);
        var systemProxy = new StubSystemProxy();
        var shell = ShellViewModelFactory.Create(
            systemProxy,
            8080,
            new ShellViewModelFactory.StubFilePickerService(),
            new ShellViewModelFactory.StubHarExporter(),
            new ShellViewModelFactory.StubHarImporter(),
            new StubToolWindowOpener(),
            new MutableUpdateNotification(),
            noCachingRule,
            breakpointConfiguration);

        shell.ToggleBreakpointCommand.Execute(null);

        await Assert.That(breakpointConfiguration.IsEnabled).IsFalse();
        await Assert.That(shell.IsBreakpointEnabled).IsFalse();
    }

    /// <summary>
    ///     The shell observable properties reflect the initial state of the supplied rule and configuration.
    /// </summary>
    [Test]
    public async Task Constructor_WithEnabledRuleAndConfiguration_ReflectsInitialState()
    {
        var noCachingRule = new MutableNoCachingRule(priority: 400, isEnabled: true);
        var breakpointConfiguration = new MutableBreakpointConfiguration(isEnabled: true);
        var systemProxy = new StubSystemProxy();
        var shell = ShellViewModelFactory.Create(
            systemProxy,
            8080,
            new ShellViewModelFactory.StubFilePickerService(),
            new ShellViewModelFactory.StubHarExporter(),
            new ShellViewModelFactory.StubHarImporter(),
            new StubToolWindowOpener(),
            new MutableUpdateNotification(),
            noCachingRule,
            breakpointConfiguration);

        await Assert.That(shell.IsNoCachingEnabled).IsTrue();
        await Assert.That(shell.IsBreakpointEnabled).IsTrue();
    }
}
