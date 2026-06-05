using Proxyfan.Domain.Certificates;
using Proxyfan.Framework.Platform;
using Proxyfan.Tests.Common;
using System.Threading;
using System.Threading.Tasks;

namespace Proxyfan.Framework.Platform.Tests;

/// <summary>
///     Integration tests for <see cref="WindowsCertificateStore" />.
///     Uses the shared persistent Proxyfan test CA from <see cref="TestPki" /> as the
///     installed-and-trusted fixture. The persistent CA is installed once per developer
///     machine (via the first <see cref="TestPki.EnsureInstalledAsync" /> call) and is
///     reused across every subsequent test run — so the Root-store install dialog only ever
///     appears on the very first run on a machine without enterprise GPO suppression.
/// </summary>
/// <remarks>
///     <para>
///         <b>Why the previous "fresh-install round-trip" tests were removed.</b>
///     </para>
///     <para>
///         Prior versions of this file included
///         <c>InstallAsync_FreshAuthority_UninstallsDuringTeardown</c> and
///         <c>InstallAsync_TeardownThrows_StillRemovesCertificate</c>. Both generated a brand
///         new root CA on every run and installed it into <c>CurrentUser\Root</c>. Windows
///         shows a non-bypassable security-warning dialog on every new thumbprint added to
///         that store. The documented suppression (<c>HKCU\Software\Policies\Microsoft\SystemCertificates\Root\ProtectedRoots\Flags=1</c>)
///         is overridden by enterprise Group Policy on the managed developer machines this
///         repository targets — meaning the dialog reliably appears on every single run of
///         those tests, blocking unattended <c>.tools\Run-Tests.ps1</c> invocations and
///         leaving stale certificates in the store when the dialog is dismissed without
///         action.
///     </para>
///     <para>
///         <b>Do not re-enable them.</b> The capabilities they used to exercise are not
///         lost:
///     </para>
///     <list type="bullet">
///         <item>
///             <description>
///                 <c>InstallAsync</c> on a not-yet-trusted CA is exercised by the very
///                 first call to <see cref="TestPki.EnsureInstalledAsync" /> on a fresh
///                 machine.
///             </description>
///         </item>
///         <item>
///             <description>
///                 <c>InstallAsync</c> on an already-trusted CA is exercised by
///                 <see cref="InstallAsync_PersistentAuthority_IsIdempotent" /> (covers the
///                 "previously-installed thumbprint" path that the fresh-install tests also
///                 ended up exercising via Windows' deduplication).
///             </description>
///         </item>
///         <item>
///             <description>
///                 <c>UninstallAsync</c> is exercised by
///                 <see cref="UninstallAsync_WhenCertificateNotInStore_CompletesWithoutError" />,
///                 which is the only branch that does not require trusting a fresh thumbprint.
///             </description>
///         </item>
///     </list>
///     <para>
///         Any future regression that motivates re-testing the fresh-CA install/uninstall
///         round-trip must be implemented either (a) under
///         <see cref="X509Store" /> indirection so it does not touch the real Root store, or
///         (b) gated behind a CI-only environment variable check — never as an
///         unconditionally-running test that mutates the developer's trust store.
///     </para>
/// </remarks>
[NotInParallel]
public sealed class WindowsCertificateStoreTests
{
    private static CertificateAuthority? _sharedAuthority;
    private static WindowsCertificateStore? _sharedStore;

    /// <summary>
    ///     Loads (or, on a brand-new machine, generates) the persistent Proxyfan test CA and
    ///     ensures it is trusted in CurrentUser\Root before any test in this class runs.
    /// </summary>
    [Before(Class)]
    public static async Task EnsurePersistentAuthorityInstalled(CancellationToken cancellationToken)
    {
        var authority = await TestPki.EnsureInstalledAsync(cancellationToken);
        _sharedAuthority = authority;
        _sharedStore = new WindowsCertificateStore();
    }

    /// <summary>
    ///     Verifies that <see cref="WindowsCertificateStore.InstallAsync" /> is idempotent
    ///     when invoked on a CA that is already in the CurrentUser\Root store: it completes
    ///     without throwing and leaves the authority marked installed.
    /// </summary>
    [Test]
    public async Task InstallAsync_PersistentAuthority_IsIdempotent(CancellationToken cancellationToken)
    {
        await Assert.That(async () => await _sharedStore!.InstallAsync(_sharedAuthority!, cancellationToken))
            .ThrowsNothing();
        await Assert.That(_sharedAuthority!.IsInstalled).IsTrue();
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
    ///     Verifies that <see cref="WindowsCertificateStore.IsInstalledAsync" /> reports the
    ///     persistent CA as installed and updates the authority's <see cref="CertificateAuthority.IsInstalled" />
    ///     flag accordingly.
    /// </summary>
    [Test]
    public async Task IsInstalledAsync_PersistentAuthority_ReturnsTrue(CancellationToken cancellationToken)
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
}
