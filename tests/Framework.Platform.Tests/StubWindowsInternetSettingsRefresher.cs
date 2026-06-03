using System.Runtime.Versioning;

namespace Proxyfan.Framework.Platform.Tests;

/// <summary>
///     Test double for <see cref="IWindowsInternetSettingsRefresher" /> that
///     records how many times <see cref="Refresh" /> was invoked.
/// </summary>
[SupportedOSPlatform("windows")]
internal sealed class StubWindowsInternetSettingsRefresher : IWindowsInternetSettingsRefresher
{
    private int _refreshCount;

    /// <summary>
    ///     Gets the number of times <see cref="Refresh" /> has been called.
    /// </summary>
    public int RefreshCount => _refreshCount;

    /// <inheritdoc />
    public void Refresh()
    {
        _refreshCount++;
    }
}
