namespace Proxyfan.Domain.Certificates;

/// <summary>
///     Represents a certificate-related domain error.
/// </summary>
public sealed record CertificateError : DomainError
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="CertificateError" /> class.
    /// </summary>
    /// <param name="message">The human-readable error description.</param>
    /// <param name="code">The machine-readable error code.</param>
    public CertificateError(string message, string code)
        : base(code, message)
    {
    }
}