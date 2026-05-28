using System;
using System.Text;
using System.Threading.Tasks;
using Proxyfan.Domain.Certificates.Provisioning;
using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.Core;

namespace Proxyfan.Domain.Certificates.Tests.Provisioning;

public sealed class CertificateProvisioningResponseBuilderTests
{
    [Test]
    public async Task Build_LandingPath_ReturnsHtmlPage()
    {
        using var certificate = CertificateTestFactory.Create("Proxyfan CA");
        var response = CertificateProvisioningResponseBuilder.Build("/", certificate);
        await Assert.That(response.ContentType).IsEqualTo(MediaTypes.TextHypertextMarkup);
        var html = Encoding.UTF8.GetString(response.Body.Span);
        await Assert.That(html).Contains("<!doctype html");
        await Assert.That(html).Contains(CertificateProvisioningResponseBuilder.MobileConfigPath);
        await Assert.That(html).Contains(CertificateProvisioningResponseBuilder.AndroidCertificatePath);
        await Assert.That(html).Contains(CertificateProvisioningResponseBuilder.CertificateDerPath);
        await Assert.That(html).Contains(CertificateProvisioningResponseBuilder.CertificatePemPath);
        await Assert.That(response.ContentDisposition).IsEqualTo(string.Empty);
    }

    [Test]
    public async Task Build_UnknownPath_ReturnsLandingPage()
    {
        using var certificate = CertificateTestFactory.Create("Proxyfan CA");
        var response = CertificateProvisioningResponseBuilder.Build("/some/unknown/path", certificate);
        await Assert.That(response.ContentType).IsEqualTo(MediaTypes.TextHypertextMarkup);
    }

    [Test]
    public async Task Build_EmptyPath_ReturnsLandingPage()
    {
        using var certificate = CertificateTestFactory.Create("Proxyfan CA");
        var response = CertificateProvisioningResponseBuilder.Build(string.Empty, certificate);
        await Assert.That(response.ContentType).IsEqualTo(MediaTypes.TextHypertextMarkup);
    }

    [Test]
    public async Task Build_PathWithQueryString_StripsQueryBeforeMatching()
    {
        using var certificate = CertificateTestFactory.Create("Proxyfan CA");
        var response = CertificateProvisioningResponseBuilder.Build(
            CertificateProvisioningResponseBuilder.CertificateDerPath + "?download=1",
            certificate);
        await Assert.That(response.ContentType).IsEqualTo(MediaTypes.ApplicationOctetStream);
    }

    [Test]
    public async Task Build_PathWithoutLeadingSlash_IsNormalized()
    {
        using var certificate = CertificateTestFactory.Create("Proxyfan CA");
        var response = CertificateProvisioningResponseBuilder.Build("proxyfan-ca.der", certificate);
        await Assert.That(response.ContentType).IsEqualTo(MediaTypes.ApplicationOctetStream);
    }

    [Test]
    public async Task Build_DerPath_ReturnsRawDerBytes()
    {
        using var certificate = CertificateTestFactory.Create("Proxyfan CA");
        var response = CertificateProvisioningResponseBuilder.Build(
            CertificateProvisioningResponseBuilder.CertificateDerPath,
            certificate);
        await Assert.That(response.ContentType).IsEqualTo(MediaTypes.ApplicationOctetStream);
        await Assert.That(response.ContentDisposition).Contains("proxyfan-ca.der");
        var expected = certificate.GetRawCertData();
        await Assert.That(response.Body.ToArray()).IsEquivalentTo(expected);
    }

    [Test]
    public async Task Build_PemPath_ReturnsPemEncodedText()
    {
        using var certificate = CertificateTestFactory.Create("Proxyfan CA");
        var response = CertificateProvisioningResponseBuilder.Build(
            CertificateProvisioningResponseBuilder.CertificatePemPath,
            certificate);
        await Assert.That(response.ContentType).IsEqualTo(MediaTypes.ApplicationXPemFile);
        await Assert.That(response.ContentDisposition).Contains("proxyfan-ca.pem");
        var pem = Encoding.ASCII.GetString(response.Body.Span);
        await Assert.That(pem).StartsWith("-----BEGIN CERTIFICATE-----");
        await Assert.That(pem).Contains("-----END CERTIFICATE-----");
    }

    [Test]
    public async Task Build_AndroidPath_ReturnsAndroidContentType()
    {
        using var certificate = CertificateTestFactory.Create("Proxyfan CA");
        var response = CertificateProvisioningResponseBuilder.Build(
            CertificateProvisioningResponseBuilder.AndroidCertificatePath,
            certificate);
        await Assert.That(response.ContentType).IsEqualTo(MediaTypes.ApplicationXX509CaCert);
        await Assert.That(response.ContentDisposition).Contains("proxyfan-ca.crt");
    }

    [Test]
    public async Task Build_MobileConfigPath_ReturnsAppleConfigurationProfile()
    {
        using var certificate = CertificateTestFactory.Create("Proxyfan CA");
        var response = CertificateProvisioningResponseBuilder.Build(
            CertificateProvisioningResponseBuilder.MobileConfigPath,
            certificate);
        await Assert.That(response.ContentType).IsEqualTo(MediaTypes.ApplicationXAppleAspenConfig);
        await Assert.That(response.ContentDisposition).Contains("proxyfan-ca.mobileconfig");
        var xml = Encoding.UTF8.GetString(response.Body.Span);
        await Assert.That(xml).StartsWith("<?xml");
        await Assert.That(xml).Contains("<!DOCTYPE plist");
        await Assert.That(xml).Contains("<key>PayloadType</key><string>Configuration</string>");
        await Assert.That(xml).Contains("<key>PayloadType</key><string>com.apple.security.root</string>");
        await Assert.That(xml).Contains("com.proxyfan.ca.profile");
    }

    [Test]
    public async Task Build_MobileConfigPath_EmbedsCertificateBase64()
    {
        using var certificate = CertificateTestFactory.Create("Proxyfan CA");
        var response = CertificateProvisioningResponseBuilder.Build(
            CertificateProvisioningResponseBuilder.MobileConfigPath,
            certificate);
        var xml = Encoding.UTF8.GetString(response.Body.Span);
        var derBase64 = Convert.ToBase64String(certificate.GetRawCertData(), Base64FormattingOptions.InsertLineBreaks);
        await Assert.That(xml).Contains(derBase64);
    }

    [Test]
    public async Task Build_DerAndAndroidPath_HaveIdenticalBytes()
    {
        using var certificate = CertificateTestFactory.Create("Proxyfan CA");
        var der = CertificateProvisioningResponseBuilder.Build(
            CertificateProvisioningResponseBuilder.CertificateDerPath,
            certificate);
        var android = CertificateProvisioningResponseBuilder.Build(
            CertificateProvisioningResponseBuilder.AndroidCertificatePath,
            certificate);
        await Assert.That(der.Body.ToArray()).IsEquivalentTo(android.Body.ToArray());
    }

    /// <summary>
    ///     A path consisting only of a query string strips to empty and falls back to the
    ///     landing page (covers the <c>withoutQuery.Length == 0</c> branch).
    /// </summary>
    [Test]
    public async Task Build_PathOnlyQueryString_ReturnsLandingPage()
    {
        using var certificate = CertificateTestFactory.Create("Proxyfan CA");
        var response = CertificateProvisioningResponseBuilder.Build("?foo=bar", certificate);
        await Assert.That(response.ContentType).IsEqualTo(MediaTypes.TextHypertextMarkup);
    }
}
