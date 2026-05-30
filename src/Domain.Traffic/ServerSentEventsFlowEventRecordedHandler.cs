namespace Proxyfan.Domain.Traffic;

/// <summary>
///     Delegate raised when a <see cref="ServerSentEventsFlow" /> appends a new event via
///     <see cref="ServerSentEventsFlow.RecordEvent" />.
/// </summary>
/// <param name="serverSentEvent">The event just appended.</param>
public delegate void ServerSentEventsFlowEventRecordedHandler(ServerSentEvent serverSentEvent);
