using Proxyfan.Client.Shell.ViewModels;
using Proxyfan.Client.Tests.Stubs;
using Proxyfan.Client.Traffic.ViewModels;
using Proxyfan.Domain;
using Proxyfan.Domain.Proxy;
using Proxyfan.Domain.Session.Har;
using Proxyfan.Domain.Traffic;
using Proxyfan.Domain.Traffic.Events;
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
        var options = new ProxyOptions { Port = port };
        var optionsMonitor = new StubOptionsMonitor<ProxyOptions>(options);
        var eventBus = new NoopEventBus();
        var trafficList = new TrafficListViewModel(eventBus, InlineUserInterfaceScheduler.Instance);
        var sourceList = new SourceListViewModel(eventBus, trafficList, InlineUserInterfaceScheduler.Instance);
        return new ShellViewModel(systemProxy, optionsMonitor, trafficList, sourceList, filePicker, harExporter, harImporter);
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
