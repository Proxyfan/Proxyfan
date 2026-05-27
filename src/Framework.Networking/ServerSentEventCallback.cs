using Proxyfan.Domain.Traffic;

namespace Proxyfan.Framework.Networking;

/// <summary>
///     Delegate invoked by <see cref="ServerSentEventsRelay" /> for every fully-parsed event.
///     Invoked synchronously from the relay loop so callers must keep work fast or marshal
///     to a background task.
/// </summary>
/// <param name="serverSentEvent">The captured Server-Sent Event.</param>
public delegate void ServerSentEventCallback(ServerSentEvent serverSentEvent);
