namespace Proxyfan.Framework.Serialization;

/// <summary>
///     A parsed gRPC trailer envelope: the numeric status code, the optional human-readable
///     status message, and the trailing metadata that accompanied the response.
/// </summary>
public sealed class RemoteProcedureCallStatus
{
    /// <summary>
    ///     Gets the gRPC status code as the typed enum (Unknown when the wire value didn't
    ///     map to a known value).
    /// </summary>
    public RemoteProcedureCallStatusCode Code { get; }

    /// <summary>
    ///     Gets the human-readable status message (from grpc-message trailer), or null when absent.
    /// </summary>
    public string? Message { get; }

    /// <summary>
    ///     Gets the raw numeric status code (preserved exactly even when unknown).
    /// </summary>
    public int RawCode { get; }

    /// <summary>
    ///     Initializes a new <see cref="RemoteProcedureCallStatus" />.
    /// </summary>
    /// <param name="rawCode">The raw numeric code.</param>
    /// <param name="code">The typed code.</param>
    /// <param name="message">The optional grpc-message text.</param>
    public RemoteProcedureCallStatus(int rawCode, RemoteProcedureCallStatusCode code, string? message)
    {
        RawCode = rawCode;
        Code = code;
        Message = message;
    }
}
