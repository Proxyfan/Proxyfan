using Proxyfan.Client.Shell.ViewModels;
using Proxyfan.Client.Tests.Stubs;
using Proxyfan.Client.Traffic.ViewModels;
using Proxyfan.Domain;
using Proxyfan.Domain.Proxy;
using Proxyfan.Domain.Rules.Rules;
using Proxyfan.Domain.Session.Har;
using Proxyfan.Domain.Traffic;
using Proxyfan.Domain.Traffic.Events;
using Proxyfan.Domain.Updates;
using Proxyfan.Presentation.Files;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Proxyfan.Client.Tests.Stubs;

/// <summary>
///     Factory helpers for constructing <see cref="ShellViewModel" /> instances with
///     the right test scaffolding (stub bus, traffic list view model).
/// </summary>
public static class ShellViewModelFactory
{
    /// <summary>
    ///     Creates a new <see cref="ShellViewModel" /> wired with the supplied
    ///     <paramref name="systemProxy" /> and a fresh stub event bus + traffic list view model.
    /// </summary>
    /// <param name="systemProxy">The system proxy stub to wire in.</param>
    /// <param name="port">The proxy port for <see cref="ProxyOptions" />.</param>
    /// <returns>A new <see cref="ShellViewModel" /> instance.</returns>
    internal static ShellViewModel Create(StubSystemProxy systemProxy, int port)
    {
        return Create(systemProxy, port, new StubFilePickerService(), new StubHarExporter(), new StubHarImporter());
    }

    /// <summary>
    ///     Creates a new <see cref="ShellViewModel" /> wired with the supplied
    ///     <paramref name="systemProxy" /> and explicit picker/exporter/importer stubs.
    /// </summary>
    /// <param name="systemProxy">The system proxy stub to wire in.</param>
    /// <param name="port">The proxy port for <see cref="ProxyOptions" />.</param>
    /// <param name="filePicker">The file picker stub.</param>
    /// <param name="harExporter">The HAR exporter stub.</param>
    /// <param name="harImporter">The HAR importer stub.</param>
    /// <returns>A new <see cref="ShellViewModel" /> instance.</returns>
    internal static ShellViewModel Create(
        StubSystemProxy systemProxy,
        int port,
        IFilePickerService filePicker,
        IHarExporter harExporter,
        IHarImporter harImporter)
    {
        var toolWindowOpener = new StubToolWindowOpener();
        return Create(systemProxy, port, filePicker, harExporter, harImporter, toolWindowOpener);
    }

    /// <summary>
    ///     Creates a new <see cref="ShellViewModel" /> wired with the supplied
    ///     <paramref name="systemProxy" /> and explicit stubs including the tool window opener.
    /// </summary>
    /// <param name="systemProxy">The system proxy stub to wire in.</param>
    /// <param name="port">The proxy port for <see cref="ProxyOptions" />.</param>
    /// <param name="filePicker">The file picker stub.</param>
    /// <param name="harExporter">The HAR exporter stub.</param>
    /// <param name="harImporter">The HAR importer stub.</param>
    /// <param name="toolWindowOpener">The tool window opener stub.</param>
    /// <returns>A new <see cref="ShellViewModel" /> instance.</returns>
    internal static ShellViewModel Create(
        StubSystemProxy systemProxy,
        int port,
        IFilePickerService filePicker,
        IHarExporter harExporter,
        IHarImporter harImporter,
        StubToolWindowOpener toolWindowOpener)
    {
        return Create(systemProxy, port, filePicker, harExporter, harImporter, toolWindowOpener, new MutableUpdateNotification());
    }

    /// <summary>
    ///     Creates a new <see cref="ShellViewModel" /> wired with the supplied
    ///     <paramref name="systemProxy" />, tool window opener, and update notification.
    /// </summary>
    /// <param name="systemProxy">The system proxy stub to wire in.</param>
    /// <param name="port">The proxy port for <see cref="ProxyOptions" />.</param>
    /// <param name="filePicker">The file picker stub.</param>
    /// <param name="harExporter">The HAR exporter stub.</param>
    /// <param name="harImporter">The HAR importer stub.</param>
    /// <param name="toolWindowOpener">The tool window opener stub.</param>
    /// <param name="updateNotification">The observable update notification used by the banner.</param>
    /// <returns>A new <see cref="ShellViewModel" /> instance.</returns>
    internal static ShellViewModel Create(
        StubSystemProxy systemProxy,
        int port,
        IFilePickerService filePicker,
        IHarExporter harExporter,
        IHarImporter harImporter,
        StubToolWindowOpener toolWindowOpener,
        MutableUpdateNotification updateNotification)
    {
        var noCachingRule = new MutableNoCachingRule(priority: 400, isEnabled: false);
        var breakpointConfiguration = new MutableBreakpointConfiguration(isEnabled: false);
        return Create(
            systemProxy,
            port,
            filePicker,
            harExporter,
            harImporter,
            toolWindowOpener,
            updateNotification,
            noCachingRule,
            breakpointConfiguration);
    }

    /// <summary>
    ///     Creates a new <see cref="ShellViewModel" /> wired with the supplied
    ///     <paramref name="systemProxy" />, tool window opener, update notification, and rule instances.
    /// </summary>
    /// <param name="systemProxy">The system proxy stub to wire in.</param>
    /// <param name="port">The proxy port for <see cref="ProxyOptions" />.</param>
    /// <param name="filePicker">The file picker stub.</param>
    /// <param name="harExporter">The HAR exporter stub.</param>
    /// <param name="harImporter">The HAR importer stub.</param>
    /// <param name="toolWindowOpener">The tool window opener stub.</param>
    /// <param name="updateNotification">The observable update notification used by the banner.</param>
    /// <param name="noCachingRule">The mutable global No-Caching rule shared with the test.</param>
    /// <param name="breakpointConfiguration">The mutable breakpoint configuration shared with the test.</param>
    /// <returns>A new <see cref="ShellViewModel" /> instance.</returns>
    internal static ShellViewModel Create(
        StubSystemProxy systemProxy,
        int port,
        IFilePickerService filePicker,
        IHarExporter harExporter,
        IHarImporter harImporter,
        StubToolWindowOpener toolWindowOpener,
        MutableUpdateNotification updateNotification,
        MutableNoCachingRule noCachingRule,
        MutableBreakpointConfiguration breakpointConfiguration)
    {
        var options = new ProxyOptions { Port = port };
        var optionsMonitor = new StubOptionsMonitor<ProxyOptions>(options);
        var eventBus = new NoopEventBus();
        var trafficList = new TrafficListViewModel(eventBus, InlineUserInterfaceScheduler.Instance);
        var sourceList = new SourceListViewModel(eventBus, trafficList, InlineUserInterfaceScheduler.Instance);
        var tabHost = new TabHostViewModel(trafficList);
        return new ShellViewModel(
            systemProxy,
            optionsMonitor,
            tabHost,
            sourceList,
            filePicker,
            harExporter,
            harImporter,
            toolWindowOpener,
            updateNotification,
            InlineUserInterfaceScheduler.Instance,
            noCachingRule,
            breakpointConfiguration);
    }

    /// <summary>
    ///     A stub <see cref="IFilePickerService" /> that returns null streams by default,
    ///     simulating a cancelled picker dialog.
    /// </summary>
    internal sealed class StubFilePickerService : IFilePickerService
    {
        public Stream? ReadStream { get; set; }
        public Stream? WriteStream { get; set; }
        public int OpenForReadCallCount { get; private set; }
        public int OpenForWriteCallCount { get; private set; }

        public Task<Stream?> OpenForReadAsync(FilePickerOpenRequest request, CancellationToken cancellationToken)
        {
            OpenForReadCallCount++;
            return Task.FromResult(ReadStream);
        }

        public Task<Stream?> OpenForWriteAsync(FilePickerSaveRequest request, CancellationToken cancellationToken)
        {
            OpenForWriteCallCount++;
            return Task.FromResult(WriteStream);
        }
    }

    /// <summary>
    ///     A stub HAR exporter that records the captured snapshot and stream it received.
    /// </summary>
    internal sealed class StubHarExporter : IHarExporter
    {
        public IReadOnlyList<TrafficFlow>? LastFlows { get; private set; }
        public Stream? LastStream { get; private set; }
        public int CallCount { get; private set; }

        public Task ExportAsync(IReadOnlyList<TrafficFlow> flows, Stream output, CancellationToken cancellationToken)
        {
            CallCount++;
            LastFlows = flows;
            LastStream = output;
            return Task.CompletedTask;
        }
    }

    /// <summary>
    ///     A stub HAR importer that returns a configurable list of flows.
    /// </summary>
    internal sealed class StubHarImporter : IHarImporter
    {
        public IReadOnlyList<TrafficFlow> ReturnFlows { get; set; } = Array.Empty<TrafficFlow>();
        public int CallCount { get; private set; }

        public Task<IReadOnlyList<TrafficFlow>> ImportAsync(Stream input, CancellationToken cancellationToken)
        {
            CallCount++;
            return Task.FromResult(ReturnFlows);
        }
    }

    private sealed class NoopEventBus : IDomainEventBus
    {
        public void Publish<TEvent>(TEvent domainEvent)
            where TEvent : IDomainEvent
        {
        }

        public IDisposable Subscribe<TEvent>(DomainEventHandler<TEvent> handler)
            where TEvent : IDomainEvent
        {
            return new NoopSubscription();
        }

        private sealed class NoopSubscription : IDisposable
        {
            public void Dispose()
            {
            }
        }
    }
}
