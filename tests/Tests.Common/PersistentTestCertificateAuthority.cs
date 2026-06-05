using Proxyfan.Domain.Certificates;
using Proxyfan.Framework.Platform;
using System;
using System.IO;
using System.Runtime.Versioning;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;

namespace Proxyfan.Tests.Common;

/// <summary>
///     Loads or creates a persistent Proxyfan test root certificate authority on disk under
///     <c>%LOCALAPPDATA%\Proxyfan\test-pki\test-ca.pfx</c> so the same CA is reused across
///     every test run on a developer machine.
/// </summary>
/// <remarks>
///     The persistent CA pattern delivers a single, stable thumbprint. Once trusted in the
///     CurrentUser\Root store, Windows skips the security-warning dialog on subsequent runs
///     because the thumbprint is already known. This is the foundation of the
///     "install once, reuse forever" contract for the test suite.
///
///     The PFX file on disk is unencrypted (test material only). It is exported with
///     <see cref="X509ContentType.Pfx" />, written atomically via temp-file rename, and loaded
///     back with <see cref="X509KeyStorageFlags.EphemeralKeySet" /> so the imported key never
///     creates a persisted key container under the user profile.
/// </remarks>
[SupportedOSPlatform("windows")]
public static class PersistentTestCertificateAuthority
{
    private const string ProxyfanFolderName = "Proxyfan";
    private const string TestPkiFolderName = "test-pki";
    private const string CertificateFileName = "test-ca.pfx";

    /// <summary>
    ///     Returns the absolute path of the persisted PFX file.
    /// </summary>
    public static string PersistedFilePath
    {
        get
        {
            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            return Path.Combine(localAppData, ProxyfanFolderName, TestPkiFolderName, CertificateFileName);
        }
    }

    /// <summary>
    ///     Loads the persisted CA when present; otherwise generates a fresh one via
    ///     <see cref="RsaCertificateGenerator" />, writes it to disk, and returns it.
    /// </summary>
    /// <param name="cancellationToken">A token that cancels generation.</param>
    /// <returns>The loaded or newly generated certificate authority.</returns>
    public static async Task<CertificateAuthority> LoadOrCreateAsync(CancellationToken cancellationToken)
    {
        var persistedFilePath = PersistedFilePath;
        if (File.Exists(persistedFilePath))
        {
            var existing = LoadFromDisk(persistedFilePath);
            return existing;
        }

        var generator = new RsaCertificateGenerator();
        var fresh = await generator.GenerateRootCertificateAuthorityAsync(cancellationToken).ConfigureAwait(false);
        WriteToDisk(fresh.Certificate, persistedFilePath);
        return fresh;
    }

    private static CertificateAuthority LoadFromDisk(string filePath)
    {
        var bytes = File.ReadAllBytes(filePath);
        var storageFlags = X509KeyStorageFlags.Exportable | X509KeyStorageFlags.EphemeralKeySet;
        var certificate = X509CertificateLoader.LoadPkcs12(bytes, string.Empty, storageFlags);
        var authority = new CertificateAuthority(certificate);
        return authority;
    }

    private static void WriteToDisk(X509Certificate2 certificate, string filePath)
    {
        var folder = Path.GetDirectoryName(filePath);
        if (folder is not null)
        {
            Directory.CreateDirectory(folder);
        }

        var pfxBytes = certificate.Export(X509ContentType.Pfx);
        var tempPath = filePath + ".tmp";
        File.WriteAllBytes(tempPath, pfxBytes);
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }

        File.Move(tempPath, filePath);
    }
}
