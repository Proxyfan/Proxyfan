using System;

namespace Proxyfan.Domain.Traffic;

/// <summary>
///     Describes a failure while sending a composed request to an upstream endpoint.
/// </summary>
public sealed record ComposerSendError : DomainError
{
    /// <summary>
    ///     Initializes a new <see cref="ComposerSendError" />.
    /// </summary>
    /// <param name="message">The human-readable failure message.</param>
    /// <param name="innerException">The underlying exception that caused the send failure.</param>
    public ComposerSendError(string message, Exception innerException)
        : base("TRAFFIC_COMPOSER_SEND_FAILED", message, innerException)
    {
    }
}
