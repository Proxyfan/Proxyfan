namespace Proxyfan.Domain.Traffic;

/// <summary>
///     Delegate raised when a <see cref="RemoteProcedureCallFlow" /> is marked closed via
///     <see cref="RemoteProcedureCallFlow.MarkClosed" />. Fires at most once per flow.
/// </summary>
public delegate void RemoteProcedureCallFlowClosedHandler();
