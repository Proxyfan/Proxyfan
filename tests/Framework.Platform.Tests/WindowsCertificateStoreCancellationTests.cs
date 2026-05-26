using Proxyfan.Domain.Certificates;
using Proxyfan.Framework.Platform;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Proxyfan.Framework.Platform.Tests;

/// <summary>
///     Cancellation-token behavior tests for <see cref="WindowsCertificateStore" />.
/// </summary>
public sealed class WindowsCertificateStoreCancellationTests
{
    /// <summary>
    ///     Verifies that <see cref="WindowsCertificateStore.InstallAsync" /> respects a
    ///     pre-cancelled token by throwing <see cref="OperationCanceledException" />.
    /// </summary>
    [Test]
    public async Task InstallAsync_WhenTokenAlreadyCancelled_Throws()
    {
        var store = new WindowsCertificateStore();
        var authority = await new RsaCertificateGenerator().GenerateRootCertificateAuthorityAsync(CancellationToken.None);
        using var cancellationSource = new CancellationTokenSource();
        await cancellationSource.CancelAsync();

        await Assert.That(async () => await store.InstallAsync(authority, cancellationSource.Token))
            .Throws<OperationCanceledException>();
    }

    /// <summary>
    ///     Verifies that <see cref="WindowsCertificateStore.IsInstalledAsync" /> respects a
    ///     pre-cancelled token by throwing <see cref="OperationCanceledException" />.
    /// </summary>
    [Test]
    public async Task IsInstalledAsync_WhenTokenAlreadyCancelled_Throws()
    {
        var store = new WindowsCertificateStore();
        var authority = await new RsaCertificateGenerator().GenerateRootCertificateAuthorityAsync(CancellationToken.None);
        using var cancellationSource = new CancellationTokenSource();
        await cancellationSource.CancelAsync();

        await Assert.That(async () => await store.IsInstalledAsync(authority, cancellationSource.Token))
            .Throws<OperationCanceledException>();
    }

    /// <summary>
    ///     Verifies that <see cref="WindowsCertificateStore.UninstallAsync" /> respects a
    ///     pre-cancelled token by throwing <see cref="OperationCanceledException" />.
    /// </summary>
    [Test]
    public async Task UninstallAsync_WhenTokenAlreadyCancelled_Throws()
    {
        var store = new WindowsCertificateStore();
        var authority = await new RsaCertificateGenerator().GenerateRootCertificateAuthorityAsync(CancellationToken.None);
        using var cancellationSource = new CancellationTokenSource();
        await cancellationSource.CancelAsync();

        await Assert.That(async () => await store.UninstallAsync(authority, cancellationSource.Token))
            .Throws<OperationCanceledException>();
    }
}