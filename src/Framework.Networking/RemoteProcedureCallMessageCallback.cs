using Proxyfan.Domain.Traffic;

namespace Proxyfan.Framework.Networking;

/// <summary>
///     Delegate invoked by <see cref="RemoteProcedureCallRelay" /> for every captured gRPC
///     message. Invoked synchronously from the relay loop so callers must keep work fast
///     or marshal to a background task.
/// </summary>
/// <param name="message">The captured gRPC message.</param>
public delegate void RemoteProcedureCallMessageCallback(RemoteProcedureCallCapturedMessage message);
