namespace Proxyfan.Domain.Certificates.Provisioning;

/// <summary>
///     The well-known media type used for an iOS configuration profile (Apple Mobile Config).
/// </summary>
public static class MediaTypes
{
    /// <summary>
    ///     <c>application/octet-stream</c>: generic binary payload (used for raw <c>DER</c> downloads).
    /// </summary>
    public const string ApplicationOctetStream = "application/octet-stream";

    /// <summary>
    ///     <c>application/x-apple-aspen-config</c>: iOS / macOS configuration profile.
    /// </summary>
    public const string ApplicationXAppleAspenConfig = "application/x-apple-aspen-config";

    /// <summary>
    ///     <c>application/x-pem-file</c>: PEM-encoded certificate payload.
    /// </summary>
    public const string ApplicationXPemFile = "application/x-pem-file";

    /// <summary>
    ///     <c>application/x-x509-ca-cert</c>: Android-friendly content type for installing a CA certificate.
    /// </summary>
    public const string ApplicationXX509CaCert = "application/x-x509-ca-cert";

    /// <summary>
    ///     <c>text/html; charset=utf-8</c>: landing page served when no specific download is requested.
    /// </summary>
    public const string TextHypertextMarkup = "text/html; charset=utf-8";
}
