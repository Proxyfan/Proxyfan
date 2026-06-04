using Microsoft.Extensions.DependencyInjection;
using Proxyfan.Client.Shell.ViewModels;
using Proxyfan.Client.Shell.Views;
using Proxyfan.Client.Tests.Stubs;
using Proxyfan.Client.Tools;
using Proxyfan.Domain.Rules.Rules;
using Proxyfan.Domain.Updates;
using Proxyfan.Presentation;
using Proxyfan.Presentation.Localization;
using System;
using System.Globalization;
using System.Resources;

namespace Proxyfan.Client.EndToEndTests.Infrastructure;

/// <summary>
///     Owns the per-test wiring of <see cref="ContainerLocator" />,
///     <see cref="LocalizationService" />, and the shell <see cref="ShellViewModel" />.
///     A test instantiates a single <see cref="TestShellEnvironment" />, drives the
///     UI through the exposed page-object members, then disposes it to reset
///     global state.
///     <para>
///         Disposing the environment resets <see cref="ContainerLocator" /> back to
///         the empty state so a subsequent test does not see stale services from a
///         previous run.
///     </para>
/// </summary>
internal sealed class TestShellEnvironment : IDisposable
{
    private readonly ServiceProvider _services;

    /// <summary>
    ///     The fully wired <see cref="ShellViewModel" /> bound to <see cref="Window" />.
    /// </summary>
    public ShellViewModel ShellViewModel { get; }

    /// <summary>
    ///     The shell <see cref="ShellWindow" /> already shown on the headless dispatcher.
    /// </summary>
    public ShellWindow Window { get; }

    /// <summary>
    ///     The tool-window opener stub. Tests assert against the
    ///     <c>OpenXxxCallCount</c> properties to verify menu commands fire.
    /// </summary>
    public StubToolWindowOpener ToolWindowOpener { get; }

    /// <summary>
    ///     The system-proxy stub. Tests assert against <c>RegisteredPorts</c> and
    ///     <c>UnregisterCount</c> to verify proxy toggle commands fire.
    /// </summary>
    public StubSystemProxy SystemProxy { get; }

    /// <summary>
    ///     The HAR exporter stub. Tests inspect <c>CallCount</c>, <c>LastFlows</c>,
    ///     and <c>LastStream</c> to verify save behaviour.
    /// </summary>
    public ShellViewModelFactory.StubHarExporter HarExporter { get; }

    /// <summary>
    ///     The HAR importer stub. Tests assign <c>ReturnFlows</c> to drive the
    ///     <c>OpenSession</c> code path.
    /// </summary>
    public ShellViewModelFactory.StubHarImporter HarImporter { get; }

    /// <summary>
    ///     The file picker stub. Tests assign <c>ReadStream</c> / <c>WriteStream</c>
    ///     to simulate user file selections.
    /// </summary>
    public ShellViewModelFactory.StubFilePickerService FilePicker { get; }

    /// <summary>
    ///     Mutable update notification used by the shell update banner.
    ///     Tests <c>Publish(...)</c> here to drive banner visibility.
    /// </summary>
    public MutableUpdateNotification UpdateNotification { get; }

    /// <summary>
    ///     Global No-Caching rule shared with the <see cref="ShellViewModel" /> toggle.
    /// </summary>
    public MutableNoCachingRule NoCachingRule { get; }

    /// <summary>
    ///     Global breakpoint configuration shared with the <see cref="ShellViewModel" /> toggle.
    /// </summary>
    public MutableBreakpointConfiguration BreakpointConfiguration { get; }

    /// <summary>
    ///     Breakpoint pause inbox shared with the shell for status-bar queue-depth updates.
    /// </summary>
    public BreakpointPauseInbox BreakpointPauseInbox { get; }

    /// <summary>
    ///     Builds the test environment and synchronously shows the
    ///     <see cref="ShellWindow" /> on the headless dispatcher. Must be called from
    ///     inside <see cref="EndToEndTestBase.RunOnUiThreadAsync(System.Func{System.Threading.Tasks.Task})" />.
    /// </summary>
    /// <param name="port">Proxy port surfaced to the <c>ShellViewModel</c>.</param>
    public TestShellEnvironment(int port = 8080)
    {
        var systemProxy = new StubSystemProxy();
        var toolWindowOpener = new StubToolWindowOpener();
        SystemProxy = systemProxy;
        ToolWindowOpener = toolWindowOpener;

        var harExporter = new ShellViewModelFactory.StubHarExporter();
        var harImporter = new ShellViewModelFactory.StubHarImporter();
        var filePicker = new ShellViewModelFactory.StubFilePickerService();
        var updateNotification = new MutableUpdateNotification();
        var noCachingRule = new MutableNoCachingRule(priority: 400, isEnabled: false);
        var breakpointConfiguration = new MutableBreakpointConfiguration(isEnabled: false);
        var breakpointPauseInbox = new BreakpointPauseInbox();
        HarExporter = harExporter;
        HarImporter = harImporter;
        FilePicker = filePicker;
        UpdateNotification = updateNotification;
        NoCachingRule = noCachingRule;
        BreakpointConfiguration = breakpointConfiguration;
        BreakpointPauseInbox = breakpointPauseInbox;

        var shellViewModel = ShellViewModelFactory.Create(
            systemProxy,
            port,
            filePicker,
            harExporter,
            harImporter,
            toolWindowOpener,
            updateNotification,
            noCachingRule,
            breakpointConfiguration,
            breakpointPauseInbox: breakpointPauseInbox);
        ShellViewModel = shellViewModel;

        var services = new ServiceCollection();
        var localizationService = new LocalizationService(CultureInfo.GetCultureInfo("en-US"));
        localizationService.RegisterManager(new ResourceManager("Proxyfan.Client.Resources.Strings", typeof(App).Assembly));
        services.AddSingleton(localizationService);
        services.AddSingleton<IToolWindowOpener>(toolWindowOpener);
        services.AddSingleton(shellViewModel);
        services.AddSingleton(shellViewModel.SourceList);
        services.AddSingleton(shellViewModel.TrafficList);
        services.AddSingleton(shellViewModel.TabHost);
        var provider = services.BuildServiceProvider();
        _services = provider;
        ContainerLocator.Reset();
        ContainerLocator.Set(() => provider);

        var window = new ShellWindow
        {
            DataContext = shellViewModel,
        };
        Window = window;
        window.Show();
    }

    /// <inheritdoc />
    public void Dispose()
    {
        Window.Close();
        ContainerLocator.Reset();
        _services.Dispose();
        ShellViewModel.Dispose();
    }
}
