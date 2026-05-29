using Proxyfan.Client.Inspector.ViewModels;
using Proxyfan.Client.Traffic.ViewModels;
using Proxyfan.Domain.Traffic;

namespace Proxyfan.Client.Tests.Stubs;

/// <summary>
///     Factory for constructing <see cref="InspectorViewModel" /> instances in tests with
///     the empty <see cref="WebSocketInspectorViewModel" /> dependencies wired up so
///     individual tests do not need to manage that boilerplate.
/// </summary>
internal static class InspectorViewModelFactory
{
    /// <summary>
    ///     Creates a new <see cref="InspectorViewModel" /> wired to the supplied
    ///     <paramref name="trafficListViewModel" /> and an empty in-memory
    ///     <see cref="WebSocketStore" />.
    /// </summary>
    /// <param name="trafficListViewModel">The traffic list view model to bind to.</param>
    /// <returns>A configured <see cref="InspectorViewModel" />.</returns>
    public static InspectorViewModel Create(TrafficListViewModel trafficListViewModel)
    {
        var webSocketStore = new WebSocketStore();
        var webSocketInspector = new WebSocketInspectorViewModel(
            trafficListViewModel,
            webSocketStore,
            InlineUserInterfaceScheduler.Instance);
        return new InspectorViewModel(trafficListViewModel, webSocketInspector);
    }

    /// <summary>
    ///     Creates a new <see cref="InspectorViewModel" /> wired to the supplied
    ///     <paramref name="trafficListViewModel" /> and the supplied
    ///     <paramref name="webSocketStore" />.
    /// </summary>
    /// <param name="trafficListViewModel">The traffic list view model to bind to.</param>
    /// <param name="webSocketStore">The WebSocket store to bind to.</param>
    /// <returns>A configured <see cref="InspectorViewModel" />.</returns>
    public static InspectorViewModel Create(
        TrafficListViewModel trafficListViewModel,
        IWebSocketStore webSocketStore)
    {
        var webSocketInspector = new WebSocketInspectorViewModel(
            trafficListViewModel,
            webSocketStore,
            InlineUserInterfaceScheduler.Instance);
        return new InspectorViewModel(trafficListViewModel, webSocketInspector);
    }
}
