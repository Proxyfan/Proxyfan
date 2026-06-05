using System;

namespace Proxyfan.Domain.Traffic;

/// <summary>
///     Represents a request replay domain error.
/// </summary>
public sealed record RequestReplayError : DomainError
{
    /// <summary>
    ///     Error code for replay cancellation.
    /// </summary>
    public const string CancelledCode = "REQUEST_REPLAY_CANCELLED";

    /// <summary>
    ///     Error code for upstream dispatch failures.
    /// </summary>
    public const string DispatchFailedCode = "REQUEST_REPLAY_DISPATCH_FAILED";

    /// <summary>
    ///     Error code for invalid delay validation failures.
    /// </summary>
    public const string InvalidDelayCode = "REQUEST_REPLAY_INVALID_DELAY";

    /// <summary>
    ///     Error code for invalid repeat count validation failures.
    /// </summary>
    public const string InvalidRepeatCountCode = "REQUEST_REPLAY_INVALID_REPEAT_COUNT";

    /// <summary>
    ///     Gets the replay attempts completed before this error occurred.
    /// </summary>
    public int CompletedCount { get; init; }

    /// <summary>
    ///     Gets a value indicating whether this error represents cancellation.
    /// </summary>
    public bool IsCancellation => string.Equals(Code, CancelledCode, StringComparison.Ordinal);

    /// <summary>
    ///     Initializes a new instance of the <see cref="RequestReplayError" /> record.
    /// </summary>
    /// <param name="code">The machine-readable error code.</param>
    /// <param name="message">The human-readable error description.</param>
    /// <param name="completedCount">How many replay attempts completed before this error occurred.</param>
    public RequestReplayError(string code, string message, int completedCount)
        : base(code, message)
    {
        CompletedCount = completedCount;
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="RequestReplayError" /> record.
    /// </summary>
    /// <param name="code">The machine-readable error code.</param>
    /// <param name="message">The human-readable error description.</param>
    /// <param name="completedCount">How many replay attempts completed before this error occurred.</param>
    /// <param name="innerException">The underlying exception that caused the error.</param>
    public RequestReplayError(string code, string message, int completedCount, Exception innerException)
        : base(code, message, innerException)
    {
        CompletedCount = completedCount;
    }
}
