using Proxyfan.Domain.Certificates;
using System.Runtime.Versioning;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;

namespace Proxyfan.Framework.Platform;

/// <summary>
///     Provides access to the current-user Windows root certificate store.
/// </summary>
[SupportedOSPlatform("windows")]
public sealed class WindowsCertificateStore : ICertificateStore
{
    /// <inheritdoc />
    public Task InstallAsync(CertificateAuthority authority, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        using var store = CreateStore();
        store.Add(authority.Certificate);
        authority.IsInstalled = true;
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<bool> IsInstalledAsync(CertificateAuthority authority, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        using var store = CreateStore();
        var matches = store.Certificates.Find(X509FindType.FindByThumbprint, authority.Certificate.Thumbprint, false);
        var isInstalled = matches.Count > 0;
        authority.IsInstalled = isInstalled;
        return Task.FromResult(isInstalled);
    }

    /// <inheritdoc />
    public Task UninstallAsync(CertificateAuthority authority, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        using var store = CreateStore();
        var matches = store.Certificates.Find(X509FindType.FindByThumbprint, authority.Certificate.Thumbprint, false);

        foreach (X509Certificate2 match in matches)
        {
            store.Remove(match);
        }

        authority.IsInstalled = false;
        return Task.CompletedTask;
    }

    private X509Store CreateStore()
    {
        var store = new X509Store(StoreName.Root, StoreLocation.CurrentUser);
        store.Open(OpenFlags.ReadWrite);
        return store;
    }
}