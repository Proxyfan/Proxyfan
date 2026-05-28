using System;
using System.IO;
using System.IO.Pipelines;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Proxyfan.Domain.Certificates.Provisioning;
using Proxyfan.Domain.Traffic;
using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.Core;

namespace Proxyfan.Framework.Networking.Tests;

public sealed class CertificateProvisioningResponderTests
{
    [Test]
    public async Task BuildResponse_LandingPath_ReturnsHtmlOk()
    {
        using var certificate = CreateSelfSignedCertificate();
        var request = CreateRequest("http://proxyfan.proxy/");
        var response = CertificateProvisioningResponder.BuildResponse(request, certificate);

        await Assert.That(response.StatusCode).IsEqualTo(200);
        await Assert.That(response.ReasonPhrase).IsEqualTo("OK");
        await Assert.That(response.Headers.Get("Content-Type")).IsEqualTo(MediaTypes.TextHypertextMarkup);
        await Assert.That(response.Headers.Get("Connection")).IsEqualTo("close");
        await Assert.That(response.Headers.Get("Cache-Control")).IsEqualTo("no-store");
        await Assert.That(response.Body.Length).IsGreaterThan(0);
    }

    [Test]
    public async Task BuildResponse_PemPath_SetsContentDispositionAttachment()
    {
        using var certificate = CreateSelfSignedCertificate();
        var request = CreateRequest("http://proxyfan.proxy/proxyfan-ca.pem");
        var response = CertificateProvisioningResponder.BuildResponse(request, certificate);

        await Assert.That(response.Headers.Get("Content-Type")).IsEqualTo(MediaTypes.ApplicationXPemFile);
        await Assert.That(response.Headers.Get("Content-Disposition")).Contains(".pem");
    }

    [Test]
    public async Task HasProvisioningTarget_HostHeaderMatchesMagicHost_ReturnsTrue()
    {
        var request = CreateRequest("http://proxyfan.proxy/");
        await Assert.That(CertificateProvisioningResponder.HasProvisioningTarget(request)).IsTrue();
    }

    [Test]
    public async Task HasProvisioningTarget_HostHeaderWithPort_ReturnsTrue()
    {
        var parameters = new HypertextTransferProtocolRequestDataParameters
        {
            Body = Array.Empty<byte>(),
            Headers = HeaderCollection.Empty.Add("Host", "PROXYFAN.PROXY:8080"),
            Method = "GET",
            RequestUri = new Uri("/index", UriKind.Relative),
            Version = "HTTP/1.1",
        };
        var request = new HypertextTransferProtocolRequestData(parameters);

        await Assert.That(CertificateProvisioningResponder.HasProvisioningTarget(request)).IsTrue();
    }

    [Test]
    public async Task HasProvisioningTarget_OtherHost_ReturnsFalse()
    {
        var request = CreateRequest("http://example.com/");
        await Assert.That(CertificateProvisioningResponder.HasProvisioningTarget(request)).IsFalse();
    }

    [Test]
    public async Task HasProvisioningTarget_MissingHostHeader_ReturnsFalse()
    {
        var parameters = new HypertextTransferProtocolRequestDataParameters
        {
            Body = Array.Empty<byte>(),
            Headers = HeaderCollection.Empty,
            Method = "GET",
            RequestUri = new Uri("/", UriKind.Relative),
            Version = "HTTP/1.1",
        };
        var request = new HypertextTransferProtocolRequestData(parameters);

        await Assert.That(CertificateProvisioningResponder.HasProvisioningTarget(request)).IsFalse();
    }

    [Test]
    public async Task WriteResponseAsync_GivenResponse_WritesHeadersAndBody()
    {
        using var certificate = CreateSelfSignedCertificate();
        var request = CreateRequest("http://proxyfan.proxy/proxyfan-ca.pem");
        var response = CertificateProvisioningResponder.BuildResponse(request, certificate);
        var pipe = new Pipe();

        await CertificateProvisioningResponder.WriteResponseAsync(pipe.Writer, response, CancellationToken.None);
        await pipe.Writer.CompleteAsync();

        using var memoryStream = new MemoryStream();
        await pipe.Reader.AsStream().CopyToAsync(memoryStream);
        var text = Encoding.ASCII.GetString(memoryStream.ToArray());

        await Assert.That(text.StartsWith("HTTP/1.1 200 OK", StringComparison.Ordinal)).IsTrue();
        await Assert.That(text).Contains("Content-Type: application/x-pem-file");
        await Assert.That(text).Contains("-----BEGIN CERTIFICATE-----");
    }

    [Test]
    public async Task BuildResponse_AbsoluteRequestUriWithMobileConfigPath_ReturnsMobileConfig()
    {
        using var certificate = CreateSelfSignedCertificate();
        var request = CreateRequest("http://proxyfan.proxy/proxyfan-ca.mobileconfig");
        var response = CertificateProvisioningResponder.BuildResponse(request, certificate);

        await Assert.That(response.Headers.Get("Content-Type")).IsEqualTo(MediaTypes.ApplicationXAppleAspenConfig);
        await Assert.That(response.Headers.Get("Content-Disposition")).Contains(".mobileconfig");
    }

    /// <summary>
    ///     Verifies that a relative request URI starting with '/' is treated as-is when
    ///     extracting the request path, exercising the relative-URI-with-leading-slash branch
    ///     of <c>ExtractPath</c>.
    /// </summary>
    [Test]
    public async Task BuildResponse_RelativeUriWithLeadingSlash_RoutesToMatchingPath()
    {
        using var certificate = CreateSelfSignedCertificate();
        var parameters = new HypertextTransferProtocolRequestDataParameters
        {
            Body = Array.Empty<byte>(),
            Headers = HeaderCollection.Empty.Add("Host", "proxyfan.proxy"),
            Method = "GET",
            RequestUri = new Uri("/proxyfan-ca.pem", UriKind.Relative),
            Version = "HTTP/1.1",
        };
        var request = new HypertextTransferProtocolRequestData(parameters);

        var response = CertificateProvisioningResponder.BuildResponse(request, certificate);

        await Assert.That(response.Headers.Get("Content-Type")).IsEqualTo(MediaTypes.ApplicationXPemFile);
    }

    /// <summary>
    ///     Verifies that a relative request URI without a leading slash gets one prepended
    ///     before path matching, exercising the relative-URI-without-leading-slash branch of
    ///     <c>ExtractPath</c>.
    /// </summary>
    [Test]
    public async Task BuildResponse_RelativeUriWithoutLeadingSlash_PrependsSlash()
    {
        using var certificate = CreateSelfSignedCertificate();
        var parameters = new HypertextTransferProtocolRequestDataParameters
        {
            Body = Array.Empty<byte>(),
            Headers = HeaderCollection.Empty.Add("Host", "proxyfan.proxy"),
            Method = "GET",
            RequestUri = new Uri("proxyfan-ca.pem", UriKind.Relative),
            Version = "HTTP/1.1",
        };
        var request = new HypertextTransferProtocolRequestData(parameters);

        var response = CertificateProvisioningResponder.BuildResponse(request, certificate);

        await Assert.That(response.Headers.Get("Content-Type")).IsEqualTo(MediaTypes.ApplicationXPemFile);
    }

    /// <summary>
    ///     Verifies that a relative request URI with an empty OriginalString falls back to
    ///     "/" before path matching, exercising the empty-raw branch of <c>ExtractPath</c>.
    /// </summary>
    [Test]
    public async Task BuildResponse_RelativeUriWithEmptyOriginalString_FallsBackToRoot()
    {
        using var certificate = CreateSelfSignedCertificate();
        var parameters = new HypertextTransferProtocolRequestDataParameters
        {
            Body = Array.Empty<byte>(),
            Headers = HeaderCollection.Empty.Add("Host", "proxyfan.proxy"),
            Method = "GET",
            RequestUri = new Uri(string.Empty, UriKind.Relative),
            Version = "HTTP/1.1",
        };
        var request = new HypertextTransferProtocolRequestData(parameters);

        var response = CertificateProvisioningResponder.BuildResponse(request, certificate);

        await Assert.That(response.Headers.Get("Content-Type")).IsEqualTo(MediaTypes.TextHypertextMarkup);
    }

    private static HypertextTransferProtocolRequestData CreateRequest(string url)
    {
        var uri = new Uri(url);
        var parameters = new HypertextTransferProtocolRequestDataParameters
        {
            Body = Array.Empty<byte>(),
            Headers = HeaderCollection.Empty.Add("Host", uri.Authority),
            Method = "GET",
            RequestUri = uri,
            Version = "HTTP/1.1",
        };
        return new HypertextTransferProtocolRequestData(parameters);
    }

    private static X509Certificate2 CreateSelfSignedCertificate()
    {
        using var key = RSA.Create(2048);
        var request = new CertificateRequest("CN=Proxyfan CA Test", key, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        request.CertificateExtensions.Add(new X509BasicConstraintsExtension(false, false, 0, true));
        request.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.DigitalSignature, true));
        request.CertificateExtensions.Add(new X509SubjectKeyIdentifierExtension(request.PublicKey, false));
        using var certificate = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(30));
        var certificateBytes = certificate.Export(X509ContentType.Pfx);
        var loaded = X509CertificateLoader.LoadPkcs12(certificateBytes, string.Empty);
        return loaded;
    }
}
