using System.Threading.Tasks;

namespace Proxyfan.Domain.Certificates.Tests;

/// <summary>
///     Tests for <see cref="CertificateError" />.
/// </summary>
public sealed class CertificateErrorTests
{
    /// <summary>
    ///     Verifies that the constructor stores the provided message and code on the base record.
    /// </summary>
    [Test]
    public async Task Constructor_WithValues_StoresMessageAndCode()
    {
        var error = new CertificateError("Certificate generation failed.", "CERT_GENERATION_FAILED");

        await Assert.That(error.Message).IsEqualTo("Certificate generation failed.");
        await Assert.That(error.Code).IsEqualTo("CERT_GENERATION_FAILED");
    }

    /// <summary>
    ///     Verifies value-semantic equality between two errors with identical message and code.
    /// </summary>
    [Test]
    public async Task Equals_WithSameMessageAndCode_ReturnsTrue()
    {
        var first = new CertificateError("Boom.", "BOOM");
        var second = new CertificateError("Boom.", "BOOM");

        await Assert.That(first).IsEqualTo(second);
        await Assert.That(first.GetHashCode()).IsEqualTo(second.GetHashCode());
    }

    /// <summary>
    ///     Verifies that errors with different codes are not equal.
    /// </summary>
    [Test]
    public async Task Equals_WithDifferentCode_ReturnsFalse()
    {
        var first = new CertificateError("Boom.", "BOOM_A");
        var second = new CertificateError("Boom.", "BOOM_B");

        await Assert.That(first).IsNotEqualTo(second);
    }
}