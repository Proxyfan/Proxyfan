using Proxyfan.Domain.Traffic.Events;
using System;

namespace Proxyfan.Domain.RemoteDevices;

/// <summary>
///     Subscribes to <see cref="RequestReceived" /> events on the domain event bus and feeds
///     them into a <see cref="RemoteDeviceTracker" /> so the remote-device panel can display
///     connected clients.
/// </summary>
public sealed class RemoteDeviceTrackerEventBridge : IDisposable
{
    private readonly IDisposable _subscription;
    private readonly RemoteDeviceTracker _tracker;

    /// <summary>
    ///     Initializes a new <see cref="RemoteDeviceTrackerEventBridge" /> instance and subscribes
    ///     it to <paramref name="eventBus" />.
    /// </summary>
    /// <param name="eventBus">The bus to subscribe to.</param>
    /// <param name="tracker">The tracker that receives request notifications.</param>
    public RemoteDeviceTrackerEventBridge(IDomainEventBus eventBus, RemoteDeviceTracker tracker)
    {
        _tracker = tracker;
        _subscription = eventBus.Subscribe<RequestReceived>(HandleRequestReceived);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _subscription.Dispose();
    }

    private void HandleRequestReceived(RequestReceived domainEvent)
    {
        var address = ClientEndPointAddress.Extract(domainEvent.ClientEndPoint);
        if (string.IsNullOrEmpty(address))
        {
            return;
        }

        var userAgent = domainEvent.Request.Headers.Get("User-Agent");
        _tracker.RecordRequest(address, userAgent);
    }
}
