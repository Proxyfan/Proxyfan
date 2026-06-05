using Proxyfan.Domain.Traffic;

namespace Proxyfan.Client.Tools;

/// <summary>
///     Resolves optional schema metadata for captured gRPC payloads.
/// </summary>
public interface IRemoteProcedureCallSchemaLibrary
{
    /// <summary>
    ///     Attempts to resolve schema metadata for a message.
    /// </summary>
    /// <param name="methodPath">The gRPC method path for the active flow.</param>
    /// <param name="direction">The message direction.</param>
    /// <returns>The schema resolution result.</returns>
    RemoteProcedureCallSchemaResolution Resolve(string? methodPath, RemoteProcedureCallDirection direction);
}
