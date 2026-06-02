using Proxyfan.Framework.Platform;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Proxyfan.Framework.Platform.Tests;

/// <summary>
///     Cancellation-token behavior tests for <see cref="WindowsSystemProxy" />.
/// </summary>
public sealed class WindowsSystemProxyCancellationTests
{
    /// <summary>
    ///     Verifies that <see cref="WindowsSystemProxy.RegisterAsync" /> respects a
    ///     pre-cancelled token by throwing <see cref="OperationCanceledException" />.
    /// </summary>
    [Test]
    public async Task RegisterAsync_WhenTokenAlreadyCancelled_Throws()
    {
        var proxy = new WindowsSystemProxy(new StubWindowsInternetSettingsRefresher());
        using var cancellationSource = new CancellationTokenSource();
        await cancellationSource.CancelAsync();

        await Assert.That(async () => await proxy.RegisterAsync(8080, cancellationSource.Token))
            .Throws<OperationCanceledException>();
    }

    /// <summary>
    ///     Verifies that <see cref="WindowsSystemProxy.UnregisterAsync" /> respects a
    ///     pre-cancelled token by throwing <see cref="OperationCanceledException" />.
    /// </summary>
    [Test]
    public async Task UnregisterAsync_WhenTokenAlreadyCancelled_Throws()
    {
        var proxy = new WindowsSystemProxy(new StubWindowsInternetSettingsRefresher());
        using var cancellationSource = new CancellationTokenSource();
        await cancellationSource.CancelAsync();

        await Assert.That(async () => await proxy.UnregisterAsync(cancellationSource.Token))
            .Throws<OperationCanceledException>();
    }
}
