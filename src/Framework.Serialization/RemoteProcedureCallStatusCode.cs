namespace Proxyfan.Framework.Serialization;

/// <summary>
///     gRPC status codes as defined by https://grpc.github.io/grpc/core/md_doc_statuscodes.html.
/// </summary>
public enum RemoteProcedureCallStatusCode
{
    /// <summary>
    ///     OK (success).
    /// </summary>
    Ok = 0,

    /// <summary>
    ///     Cancelled — operation cancelled by the caller.
    /// </summary>
    Cancelled = 1,

    /// <summary>
    ///     Unknown error.
    /// </summary>
    Unknown = 2,

    /// <summary>
    ///     InvalidArgument — caller specified an invalid argument.
    /// </summary>
    InvalidArgument = 3,

    /// <summary>
    ///     DeadlineExceeded — deadline expired before operation could complete.
    /// </summary>
    DeadlineExceeded = 4,

    /// <summary>
    ///     NotFound — requested entity was not found.
    /// </summary>
    NotFound = 5,

    /// <summary>
    ///     AlreadyExists — entity caller attempted to create already exists.
    /// </summary>
    AlreadyExists = 6,

    /// <summary>
    ///     PermissionDenied — caller doesn't have permission.
    /// </summary>
    PermissionDenied = 7,

    /// <summary>
    ///     ResourceExhausted — resource (e.g. quota) has been exhausted.
    /// </summary>
    ResourceExhausted = 8,

    /// <summary>
    ///     FailedPrecondition — operation rejected because preconditions aren't satisfied.
    /// </summary>
    FailedPrecondition = 9,

    /// <summary>
    ///     Aborted — operation aborted (typically due to concurrency).
    /// </summary>
    Aborted = 10,

    /// <summary>
    ///     OutOfRange — operation attempted past valid range.
    /// </summary>
    OutOfRange = 11,

    /// <summary>
    ///     Unimplemented — operation not implemented or supported.
    /// </summary>
    Unimplemented = 12,

    /// <summary>
    ///     Internal — internal server error.
    /// </summary>
    Internal = 13,

    /// <summary>
    ///     Unavailable — service is currently unavailable.
    /// </summary>
    Unavailable = 14,

    /// <summary>
    ///     DataLoss — unrecoverable data loss or corruption.
    /// </summary>
    DataLoss = 15,

    /// <summary>
    ///     Unauthenticated — request does not have valid authentication credentials.
    /// </summary>
    Unauthenticated = 16,
}
