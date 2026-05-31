namespace Proxyfan.Domain.Traffic;

/// <summary>
///     Delegate raised when a <see cref="RemoteProcedureCallFlow" /> appends a new captured
///     message via <see cref="RemoteProcedureCallFlow.RecordMessage" />.
/// </summary>
/// <param name="message">The message just appended.</param>
public delegate void RemoteProcedureCallFlowMessageRecordedHandler(RemoteProcedureCallCapturedMessage message);
