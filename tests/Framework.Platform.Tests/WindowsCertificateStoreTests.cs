using Proxyfan.Domain.Certificates;
using Proxyfan.Framework.Platform;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;

namespace Proxyfan.Framework.Platform.Tests;

/// <summary>
///     Integration tests for <see cref="WindowsCertificateStore" />.
///     The Proxyfan test root CA is installed into the current-user Root store on the first run
///     and reused on all subsequent runs — zero prompts after the very first approval.
/// </summary>
[NotInParallel]
public sealed class WindowsCertificateStoreTests
{
    private const string SubjectName = "CN=Proxyfan Certificate Authority";

    private static CertificateAuthority? _sharedAuthority;
    private static WindowsCertificateStore? _sharedStore;

    /// <summary>
    ///     Ensures the Proxyfan root CA is trusted. Reuses an existing installation when present
    ///     so that the user is never prompted more than once across all test runs.
    /// </summary>
    [Before(Class)]
    public static async Task EnsureSharedAuthorityTrusted(CancellationToken cancellationToken)
    {
        var store = new WindowsCertificateStore();
        var authority = FindExistingAuthority() ?? await GenerateAndInstallAsync(store, cancellationToken);
        _sharedAuthority = authority;
        _sharedStore = store;
    }

    /// <summary>
    ///     Verifies that <see cref="WindowsCertificateStore.InstallAsync" /> marks the authority
    ///     as installed and makes it findable in the current-user root store.
    /// </summary>
    [Test]
    public async Task InstallAsync_WhenCalled_CertificateIsInStore(CancellationToken cancellationToken)
    {
        var isInstalled = await _sharedStore!.IsInstalledAsync(_sharedAuthority!, cancellationToken);

        await Assert.That(isInstalled).IsTrue();
        await Assert.That(_sharedAuthority!.IsInstalled).IsTrue();
    }

    /// <summary>
    ///     Verifies that <see cref="WindowsCertificateStore.IsInstalledAsync" /> returns
    ///     <see langword="false" /> for a certificate authority that was never installed.
    /// </summary>
    [Test]
    public async Task IsInstalledAsync_WhenCertificateNotInstalled_ReturnsFalse(CancellationToken cancellationToken)
    {
        var generator = new RsaCertificateGenerator();
        var uninstalledAuthority = await generator.GenerateRootCertificateAuthorityAsync(cancellationToken);
        var isInstalled = await _sharedStore!.IsInstalledAsync(uninstalledAuthority, cancellationToken);

        await Assert.That(isInstalled).IsFalse();
        await Assert.That(uninstalledAuthority.IsInstalled).IsFalse();
    }

    /// <summary>
    ///     Verifies that calling <see cref="WindowsCertificateStore.UninstallAsync" /> on a
    ///     certificate that is not in the store completes without error.
    /// </summary>
    [Test]
    public async Task UninstallAsync_WhenCertificateNotInStore_CompletesWithoutError(CancellationToken cancellationToken)
    {
        var generator = new RsaCertificateGenerator();
        var notInstalledAuthority = await generator.GenerateRootCertificateAuthorityAsync(cancellationToken);

        await Assert.That(async () => await _sharedStore!.UninstallAsync(notInstalledAuthority, cancellationToken))
            .ThrowsNothing();
    }

    /// <summary>
    ///     Verifies that re-installing the already-trusted Proxyfan CA does not throw and does not
    ///     prompt the user (because Windows recognizes the existing thumbprint).
    /// </summary>
    [Test]
    public async Task InstallAsync_WhenAlreadyInstalled_CompletesWithoutError(CancellationToken cancellationToken)
    {
        await Assert.That(async () => await _sharedStore!.InstallAsync(_sharedAuthority!, cancellationToken))
            .ThrowsNothing();
        await Assert.That(_sharedAuthority!.IsInstalled).IsTrue();
    }

    private static CertificateAuthority? FindExistingAuthority()
    {
        using var rootStore = new X509Store(StoreName.Root, StoreLocation.CurrentUser);
        rootStore.Open(OpenFlags.ReadOnly);
        var matches = rootStore.Certificates.Find(X509FindType.FindBySubjectDistinguishedName, SubjectName, false);

        if (matches.Count == 0)
        {
            return null;
        }

        var existingAuthority = new CertificateAuthority(matches[0]);
        existingAuthority.IsInstalled = true;
        return existingAuthority;
    }

    private static async Task<CertificateAuthority> GenerateAndInstallAsync(
        WindowsCertificateStore store,
        CancellationToken cancellationToken)
    {
        var generator = new RsaCertificateGenerator();
        var authority = await generator.GenerateRootCertificateAuthorityAsync(cancellationToken);
        await store.InstallAsync(authority, cancellationToken);
        return authority;
    }
}