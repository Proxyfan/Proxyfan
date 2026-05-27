using System;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace Proxyfan.Domain.Certificates.Provisioning;

/// <summary>
///     Builds the response payloads served by the certificate provisioning landing page
///     (HTML index, PEM, DER, and Apple <c>.mobileconfig</c> downloads). Pure: no I/O, no
///     networking — pass in the root authority certificate and a logical path to receive
///     the bytes and headers to write to the wire.
/// </summary>
public static class CertificateProvisioningResponseBuilder
{
    /// <summary>
    ///     The canonical path that returns the certificate with the Android-friendly content
    ///     type so Chrome on Android offers the install-CA flow.
    /// </summary>
    public const string AndroidCertificatePath = "/proxyfan-ca.crt";

    /// <summary>
    ///     The canonical path that returns the certificate as DER-encoded bytes.
    /// </summary>
    public const string CertificateDerPath = "/proxyfan-ca.der";

    /// <summary>
    ///     The canonical path that returns the certificate as a PEM-encoded text file.
    /// </summary>
    public const string CertificatePemPath = "/proxyfan-ca.pem";

    /// <summary>
    ///     The canonical path that returns the landing HTML page.
    /// </summary>
    public const string LandingPagePath = "/";

    /// <summary>
    ///     The canonical path that returns the certificate inside an Apple
    ///     <c>.mobileconfig</c> profile (for iOS / macOS one-tap install).
    /// </summary>
    public const string MobileConfigPath = "/proxyfan-ca.mobileconfig";

    /// <summary>
    ///     Returns the response for the supplied <paramref name="path" />. When the path is
    ///     unrecognized the landing page is returned.
    /// </summary>
    /// <param name="path">The request path (case-insensitive, leading-slash form).</param>
    /// <param name="certificate">The root certificate authority certificate.</param>
    /// <returns>The response payload to serve.</returns>
    public static CertificateProvisioningResponse Build(string path, X509Certificate2 certificate)
    {
        var normalized = NormalizePath(path);
        if (string.Equals(normalized, CertificateDerPath, StringComparison.OrdinalIgnoreCase))
        {
            return BuildDer(certificate);
        }

        if (string.Equals(normalized, CertificatePemPath, StringComparison.OrdinalIgnoreCase))
        {
            return BuildPem(certificate);
        }

        if (string.Equals(normalized, AndroidCertificatePath, StringComparison.OrdinalIgnoreCase))
        {
            return BuildAndroidCertificate(certificate);
        }

        if (string.Equals(normalized, MobileConfigPath, StringComparison.OrdinalIgnoreCase))
        {
            return BuildMobileConfig(certificate);
        }

        return BuildLandingPage(certificate);
    }

    private static CertificateProvisioningResponse BuildAndroidCertificate(X509Certificate2 certificate)
    {
        var der = certificate.Export(X509ContentType.Cert);
        var body = new ReadOnlyMemory<byte>(der);
        var response = new CertificateProvisioningResponse(
            body,
            MediaTypes.ApplicationXX509CaCert,
            "attachment; filename=\"proxyfan-ca.crt\"");
        return response;
    }

    private static CertificateProvisioningResponse BuildDer(X509Certificate2 certificate)
    {
        var der = certificate.Export(X509ContentType.Cert);
        var body = new ReadOnlyMemory<byte>(der);
        var response = new CertificateProvisioningResponse(
            body,
            MediaTypes.ApplicationOctetStream,
            "attachment; filename=\"proxyfan-ca.der\"");
        return response;
    }

    private static CertificateProvisioningResponse BuildLandingPage(X509Certificate2 certificate)
    {
        var fingerprint = certificate.GetCertHashString(System.Security.Cryptography.HashAlgorithmName.SHA256);
        var markup = new StringBuilder();
        markup.Append("<!doctype html><html lang=\"en\"><head><meta charset=\"utf-8\">");
        markup.Append("<meta name=\"viewport\" content=\"width=device-width, initial-scale=1\">");
        markup.Append("<title>Proxyfan certificate installer</title>");
        markup.Append("<style>body{font-family:system-ui,Segoe UI,Arial,sans-serif;max-width:36rem;margin:2rem auto;padding:0 1rem;line-height:1.4;}");
        markup.Append("h1{font-size:1.5rem;}");
        markup.Append("a.btn{display:block;margin:0.5rem 0;padding:0.75rem 1rem;background:#0078d4;color:#fff;text-decoration:none;border-radius:0.25rem;text-align:center;}");
        markup.Append("code{font-family:Consolas,Menlo,monospace;font-size:0.9rem;word-break:break-all;}");
        markup.Append("</style></head><body>");
        markup.Append("<h1>Install Proxyfan root certificate</h1>");
        markup.Append("<p>Pick the download that matches your device, then follow the operating system's install prompts. After installing, mark the certificate as trusted for SSL/TLS in your system trust store.</p>");
        markup.Append("<a class=\"btn\" href=\"").Append(MobileConfigPath).Append("\">iOS / macOS profile (.mobileconfig)</a>");
        markup.Append("<a class=\"btn\" href=\"").Append(AndroidCertificatePath).Append("\">Android certificate (.crt)</a>");
        markup.Append("<a class=\"btn\" href=\"").Append(CertificateDerPath).Append("\">DER (Windows / Linux)</a>");
        markup.Append("<a class=\"btn\" href=\"").Append(CertificatePemPath).Append("\">PEM (curl / scripts)</a>");
        markup.Append("<p><strong>Fingerprint (SHA-256):</strong> <code>").Append(fingerprint).Append("</code></p>");
        markup.Append("</body></html>");
        var bytes = Encoding.UTF8.GetBytes(markup.ToString());
        var body = new ReadOnlyMemory<byte>(bytes);
        var response = new CertificateProvisioningResponse(body, MediaTypes.TextHypertextMarkup, string.Empty);
        return response;
    }

    private static CertificateProvisioningResponse BuildMobileConfig(X509Certificate2 certificate)
    {
        var derBase64 = Convert.ToBase64String(certificate.Export(X509ContentType.Cert), Base64FormattingOptions.InsertLineBreaks);
        var fingerprint = certificate.GetCertHashString(System.Security.Cryptography.HashAlgorithmName.SHA256);
        var payloadUuid = Guid.NewGuid().ToString("D").ToUpperInvariant();
        var profileUuid = Guid.NewGuid().ToString("D").ToUpperInvariant();
        var xml = new StringBuilder();
        xml.Append("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
        xml.Append("<!DOCTYPE plist PUBLIC \"-//Apple//DTD PLIST 1.0//EN\" \"http://www.apple.com/DTDs/PropertyList-1.0.dtd\">");
        xml.Append("<plist version=\"1.0\"><dict>");
        xml.Append("<key>PayloadContent</key><array><dict>");
        xml.Append("<key>PayloadCertificateFileName</key><string>proxyfan-ca.cer</string>");
        xml.Append("<key>PayloadContent</key><data>").Append(derBase64).Append("</data>");
        xml.Append("<key>PayloadDescription</key><string>Adds the Proxyfan root certificate authority.</string>");
        xml.Append("<key>PayloadDisplayName</key><string>Proxyfan certificate authority</string>");
        xml.Append("<key>PayloadIdentifier</key><string>com.proxyfan.ca.payload</string>");
        xml.Append("<key>PayloadType</key><string>com.apple.security.root</string>");
        xml.Append("<key>PayloadUUID</key><string>").Append(payloadUuid).Append("</string>");
        xml.Append("<key>PayloadVersion</key><integer>1</integer>");
        xml.Append("</dict></array>");
        xml.Append("<key>PayloadDescription</key><string>Install the Proxyfan root certificate authority. Fingerprint (SHA-256): ").Append(fingerprint).Append("</string>");
        xml.Append("<key>PayloadDisplayName</key><string>Proxyfan certificate authority</string>");
        xml.Append("<key>PayloadIdentifier</key><string>com.proxyfan.ca.profile</string>");
        xml.Append("<key>PayloadOrganization</key><string>Proxyfan</string>");
        xml.Append("<key>PayloadRemovalDisallowed</key><false/>");
        xml.Append("<key>PayloadScope</key><string>User</string>");
        xml.Append("<key>PayloadType</key><string>Configuration</string>");
        xml.Append("<key>PayloadUUID</key><string>").Append(profileUuid).Append("</string>");
        xml.Append("<key>PayloadVersion</key><integer>1</integer>");
        xml.Append("</dict></plist>");
        var bytes = Encoding.UTF8.GetBytes(xml.ToString());
        var body = new ReadOnlyMemory<byte>(bytes);
        var response = new CertificateProvisioningResponse(
            body,
            MediaTypes.ApplicationXAppleAspenConfig,
            "attachment; filename=\"proxyfan-ca.mobileconfig\"");
        return response;
    }

    private static CertificateProvisioningResponse BuildPem(X509Certificate2 certificate)
    {
        var pem = certificate.ExportCertificatePem();
        var bytes = Encoding.ASCII.GetBytes(pem);
        var body = new ReadOnlyMemory<byte>(bytes);
        var response = new CertificateProvisioningResponse(
            body,
            MediaTypes.ApplicationXPemFile,
            "attachment; filename=\"proxyfan-ca.pem\"");
        return response;
    }

    private static string NormalizePath(string path)
    {
        if (string.IsNullOrEmpty(path))
        {
            return LandingPagePath;
        }

        var questionMarkIndex = path.IndexOf('?');
        var withoutQuery = questionMarkIndex < 0 ? path : path[..questionMarkIndex];
        if (withoutQuery.Length == 0)
        {
            return LandingPagePath;
        }

        if (withoutQuery[0] != '/')
        {
            return "/" + withoutQuery;
        }

        return withoutQuery;
    }
}
