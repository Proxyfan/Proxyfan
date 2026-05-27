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
}
