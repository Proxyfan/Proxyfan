using Proxyfan.Domain.Certificates;
using Proxyfan.Framework.Platform;
using System;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;

namespace Proxyfan.Framework.Platform.Tests;

/// <summary>
///     Integration tests for <see cref="WindowsCertificateStore" />.
///     Each test run generates and installs a fresh root CA into the CurrentUser Root store,
///     then always removes it during teardown — even when a test body throws.
/// </summary>
[NotInParallel]
public sealed class WindowsCertificateStoreTests
{
    private const string SubjectName = "CN=Proxyfan Certificate Authority";

    private static CertificateAuthority? _sharedAuthority;
    private static WindowsCertificateStore? _sharedStore;

    /// <summary>
    ///     Removes the certificate installed by this test class from the CurrentUser Root store.
    ///     Uses <see cref="CancellationToken.None" /> so a cancelled test run cannot prevent
    ///     the Root store from being cleaned up.
    /// </summary>
    [After(Class)]
    public static async Task CleanupSharedAuthority(CancellationToken cancellationToken)
    {
        if (_sharedAuthority is null || _sharedStore is null)
        {
            return;
        }

        try
        {
            await _sharedStore.UninstallAsync(_sharedAuthority, CancellationToken.None);
        }
        finally
        {
            _sharedAuthority = null;
            _sharedStore = null;
        }
    }

    /// <summary>
    ///     Fails fast if a leftover certificate from a previous test run is detected in the
    ///     CurrentUser Root store, then generates and installs a fresh root CA for all tests
    ///     in this class.
    /// </summary>
    [Before(Class)]
    public static async Task EnsureSharedAuthorityTrusted(CancellationToken cancellationToken)
    {
        if (FindExistingAuthority() is not null)
        {
            throw new InvalidOperationException(
                $"A certificate with subject '{SubjectName}' already exists in the CurrentUser Root store. " +
                "A previous test run may not have completed its teardown. " +
                "Remove the certificate manually and re-run the tests.");
        }

        var store = new WindowsCertificateStore();
        var authority = await GenerateAndInstallAsync(store, cancellationToken);
        _sharedAuthority = authority;
        _sharedStore = store;
    }

    /// <summary>
    ///     Verifies that a freshly installed certificate is removed when teardown uses
    ///     <see langword="try" />/<see langword="finally" /> — even on an assertion failure.
    /// </summary>
    [Test]
    public async Task InstallAsync_FreshAuthority_UninstallsDuringTeardown(CancellationToken cancellationToken)
    {
        var store = new WindowsCertificateStore();
        var generator = new RsaCertificateGenerator();
        var authority = await generator.GenerateRootCertificateAuthorityAsync(cancellationToken);

        try
        {
            await store.InstallAsync(authority, cancellationToken);
            await Assert.That(await store.IsInstalledAsync(authority, cancellationToken)).IsTrue();
        }
        finally
        {
            await store.UninstallAsync(authority, CancellationToken.None);
        }

        await Assert.That(await store.IsInstalledAsync(authority, CancellationToken.None)).IsFalse();
    }

    /// <summary>
    ///     Verifies that a certificate is removed from the Root store even when the test body
    ///     throws — demonstrating that <see langword="try" />/<see langword="finally" /> teardown
    ///     is resilient to failures.
    /// </summary>
    [Test]
    public async Task InstallAsync_TeardownThrows_StillRemovesCertificate(CancellationToken cancellationToken)
    {
        var store = new WindowsCertificateStore();
        var generator = new RsaCertificateGenerator();
        var authority = await generator.GenerateRootCertificateAuthorityAsync(cancellationToken);
        Exception? caughtException = null;

        try
        {
            await store.InstallAsync(authority, cancellationToken);
            await Assert.That(await store.IsInstalledAsync(authority, cancellationToken)).IsTrue();
            throw new InvalidOperationException("Simulated test-body failure.");
        }
        catch (InvalidOperationException ex)
        {
            caughtException = ex;
        }
        finally
        {
            await store.UninstallAsync(authority, CancellationToken.None);
        }

        await Assert.That(caughtException).IsNotNull();
        await Assert.That(await store.IsInstalledAsync(authority, CancellationToken.None)).IsFalse();
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