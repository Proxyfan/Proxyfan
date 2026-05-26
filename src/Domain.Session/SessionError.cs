namespace Proxyfan.Domain.Session;

/// <summary>
///     Represents a session-related domain error.
/// </summary>
public sealed record SessionError : DomainError
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="SessionError" /> record.
    /// </summary>
    /// <param name="code">The machine-readable error code.</param>
    /// <param name="message">The human-readable error description.</param>
    public SessionError(string code, string message)
        : base(code, message)
    {
    }
}
