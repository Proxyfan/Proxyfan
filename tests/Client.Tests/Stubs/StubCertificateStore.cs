using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Proxyfan.Domain.Certificates;

namespace Proxyfan.Client.Tests.Stubs;

/// <summary>
///     A stub <see cref="ICertificateStore" /> that tracks installed authorities by thumbprint
///     in memory so tests can verify install/uninstall behavior.
/// </summary>
internal sealed class StubCertificateStore : ICertificateStore
{
    private readonly HashSet<string> _installedThumbprints;
    public int? ThrowOnInstallCallNumber { get; set; }
    public int? ThrowOnUninstallCallNumber { get; set; }
    public int InstallCallCount { get; private set; }
    public int UninstallCallCount { get; private set; }
    public int IsInstalledCallCount { get; private set; }

    /// <summary>
    ///     Initializes a new <see cref="StubCertificateStore" />.
    /// </summary>
    public StubCertificateStore()
    {
        var installedThumbprints = new HashSet<string>();
        _installedThumbprints = installedThumbprints;
    }

    /// <inheritdoc />
    public Task InstallAsync(CertificateAuthority authority, CancellationToken cancellationToken)
    {
        InstallCallCount++;
        if (ThrowOnInstallCallNumber == InstallCallCount)
        {
            throw new InvalidOperationException("Install failure");
        }

        _installedThumbprints.Add(authority.Certificate.Thumbprint);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<bool> IsInstalledAsync(CertificateAuthority authority, CancellationToken cancellationToken)
    {
        IsInstalledCallCount++;
        var installed = _installedThumbprints.Contains(authority.Certificate.Thumbprint);
        return Task.FromResult(installed);
    }

    /// <inheritdoc />
    public Task UninstallAsync(CertificateAuthority authority, CancellationToken cancellationToken)
    {
        UninstallCallCount++;
        if (ThrowOnUninstallCallNumber == UninstallCallCount)
        {
            throw new InvalidOperationException("Uninstall failure");
        }

        _installedThumbprints.Remove(authority.Certificate.Thumbprint);
        return Task.CompletedTask;
    }
}
