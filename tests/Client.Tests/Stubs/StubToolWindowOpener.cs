using Proxyfan.Client.Tools;

namespace Proxyfan.Client.Tests.Stubs;

/// <summary>
///     Stub <see cref="IToolWindowOpener" /> that records each call without actually opening any window.
/// </summary>
public sealed class StubToolWindowOpener : IToolWindowOpener
{
    /// <summary>
    ///     Gets the number of times <see cref="OpenAllowList" /> was invoked.
    /// </summary>
    public int OpenAllowListCallCount { get; private set; }

    /// <summary>
    ///     Gets the number of times <see cref="OpenBlockList" /> was invoked.
    /// </summary>
    public int OpenBlockListCallCount { get; private set; }

    /// <summary>
    ///     Gets the number of times <see cref="OpenMapLocal" /> was invoked.
    /// </summary>
    public int OpenMapLocalCallCount { get; private set; }

    /// <summary>
    ///     Gets the number of times <see cref="OpenMapRemote" /> was invoked.
    /// </summary>
    public int OpenMapRemoteCallCount { get; private set; }

    /// <summary>
    ///     Gets the number of times <see cref="OpenThrottle" /> was invoked.
    /// </summary>
    public int OpenThrottleCallCount { get; private set; }

    /// <inheritdoc />
    public void OpenAllowList()
    {
        OpenAllowListCallCount++;
    }

    /// <inheritdoc />
    public void OpenBlockList()
    {
        OpenBlockListCallCount++;
    }

    /// <inheritdoc />
    public void OpenMapLocal()
    {
        OpenMapLocalCallCount++;
    }

    /// <inheritdoc />
    public void OpenMapRemote()
    {
        OpenMapRemoteCallCount++;
    }

    /// <inheritdoc />
    public void OpenThrottle()
    {
        OpenThrottleCallCount++;
    }
}
