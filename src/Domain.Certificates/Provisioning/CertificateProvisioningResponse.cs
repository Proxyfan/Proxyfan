using System;

namespace Proxyfan.Domain.Certificates.Provisioning;

/// <summary>
///     Represents a single HTTP response served by the certificate provisioning page,
///     containing the body bytes and the content-type header value.
/// </summary>
public sealed class CertificateProvisioningResponse
{
    /// <summary>
    ///     Gets the response body bytes.
    /// </summary>
    public ReadOnlyMemory<byte> Body { get; }

    /// <summary>
    ///     Gets the value to use for the <c>Content-Disposition</c> header (or empty when no
    ///     disposition should be applied — e.g. for the HTML landing page).
    /// </summary>
    public string ContentDisposition { get; }

    /// <summary>
    ///     Gets the MIME content type for the response body.
    /// </summary>
    public string ContentType { get; }

    /// <summary>
    ///     Initializes a new <see cref="CertificateProvisioningResponse" /> instance.
    /// </summary>
    /// <param name="body">The response body bytes.</param>
    /// <param name="contentType">The MIME content type.</param>
    /// <param name="contentDisposition">The optional <c>Content-Disposition</c> value (empty when not used).</param>
    public CertificateProvisioningResponse(ReadOnlyMemory<byte> body, string contentType, string contentDisposition)
    {
        Body = body;
        ContentDisposition = contentDisposition;
        ContentType = contentType;
    }
}
