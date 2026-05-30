namespace Proxyfan.Domain.Traffic;

/// <summary>
///     Delegate raised when a <see cref="ServerSentEventsFlow" /> is marked closed via
///     <see cref="ServerSentEventsFlow.MarkClosed" />. Fires at most once per flow.
/// </summary>
public delegate void ServerSentEventsFlowClosedHandler();
