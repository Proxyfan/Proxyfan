using Proxyfan.Domain.Certificates;
using Proxyfan.Framework.Platform;
using System.Runtime.Versioning;
using System.Threading;
using System.Threading.Tasks;

namespace Proxyfan.Tests.Common;

/// <summary>
///     Orchestrates the one-time setup of the shared Proxyfan test public key infrastructure
///     (test PKI). Call <see cref="EnsureInstalledAsync" /> from a TUnit <c>Before(Class)</c>
///     hook in any test class that needs a real, trusted CA in the CurrentUser\Root store.
/// </summary>
/// <remarks>
///     The orchestrator is intentionally idempotent and safe to call from many test classes
///     concurrently: it loads the persistent CA from disk (or generates one on the first
///     ever run), installs it into CurrentUser\Root only if not already trusted, and returns
///     the authority for tests to use as both a signing CA and a trusted server-cert root.
///
///     On a developer machine the first invocation prompts once for the new thumbprint;
///     every subsequent test run on that machine reuses the persisted thumbprint and runs
///     fully unattended.
/// </remarks>
[SupportedOSPlatform("windows")]
public static class TestPki
{
    /// <summary>
    ///     Ensures the persistent Proxyfan test CA exists on disk and is trusted in the
    ///     CurrentUser\Root store. Returns the loaded authority so tests can sign leaf
    ///     certificates with it.
    /// </summary>
    /// <param name="cancellationToken">A token that cancels generation and install.</param>
    /// <returns>The persistent certificate authority, trusted in CurrentUser\Root.</returns>
    public static async Task<CertificateAuthority> EnsureInstalledAsync(CancellationToken cancellationToken)
    {
        WindowsRootCertificatePromptSuppressor.Suppress();
        var authority = await PersistentTestCertificateAuthority.LoadOrCreateAsync(cancellationToken).ConfigureAwait(false);
        var store = new WindowsCertificateStore();
        var alreadyInstalled = await store.IsInstalledAsync(authority, cancellationToken).ConfigureAwait(false);
        if (!alreadyInstalled)
        {
            await store.InstallAsync(authority, cancellationToken).ConfigureAwait(false);
        }

        return authority;
    }
}
