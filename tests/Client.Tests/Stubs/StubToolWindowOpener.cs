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
    ///     Gets the number of times <see cref="OpenBreakpoint" /> was invoked.
    /// </summary>
    public int OpenBreakpointCallCount { get; private set; }

    /// <summary>
    ///     Gets the number of times <see cref="OpenCertificateManager" /> was invoked.
    /// </summary>
    public int OpenCertificateManagerCallCount { get; private set; }

    /// <summary>
    ///     Gets the number of times <see cref="OpenComposer" /> was invoked.
    /// </summary>
    public int OpenComposerCallCount { get; private set; }

    /// <summary>
    ///     Gets the number of times <see cref="OpenDomainNameSystemSpoofing" /> was invoked.
    /// </summary>
    public int OpenDomainNameSystemSpoofingCallCount { get; private set; }

    /// <summary>
    ///     Gets the number of times <see cref="OpenMapLocal" /> was invoked.
    /// </summary>
    public int OpenMapLocalCallCount { get; private set; }

    /// <summary>
    ///     Gets the number of times <see cref="OpenMapRemote" /> was invoked.
    /// </summary>
    public int OpenMapRemoteCallCount { get; private set; }

    /// <summary>
    ///     Gets the number of times <see cref="OpenPluginManager" /> was invoked.
    /// </summary>
    public int OpenPluginManagerCallCount { get; private set; }

    /// <summary>
    ///     Gets the number of times <see cref="OpenPreferences" /> was invoked.
    /// </summary>
    public int OpenPreferencesCallCount { get; private set; }

    /// <summary>
    ///     Gets the number of times <see cref="OpenRemoteDevices" /> was invoked.
    /// </summary>
    public int OpenRemoteDevicesCallCount { get; private set; }

    /// <summary>
    ///     Gets the number of times <see cref="OpenReverseProxy" /> was invoked.
    /// </summary>
    public int OpenReverseProxyCallCount { get; private set; }

    /// <summary>
    ///     Gets the number of times <see cref="OpenScripting" /> was invoked.
    /// </summary>
    public int OpenScriptingCallCount { get; private set; }

    /// <summary>
    ///     Gets the number of times <see cref="OpenSecureSocketsLayerProxying" /> was invoked.
    /// </summary>
    public int OpenSecureSocketsLayerProxyingCallCount { get; private set; }

    /// <summary>
    ///     Gets the number of times <see cref="OpenTheme" /> was invoked.
    /// </summary>
    public int OpenThemeCallCount { get; private set; }

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
    public void OpenBreakpoint()
    {
        OpenBreakpointCallCount++;
    }

    /// <inheritdoc />
    public void OpenCertificateManager()
    {
        OpenCertificateManagerCallCount++;
    }

    /// <inheritdoc />
    public void OpenComposer()
    {
        OpenComposerCallCount++;
    }

    /// <inheritdoc />
    public void OpenDomainNameSystemSpoofing()
    {
        OpenDomainNameSystemSpoofingCallCount++;
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
    public void OpenPluginManager()
    {
        OpenPluginManagerCallCount++;
    }

    /// <inheritdoc />
    public void OpenPreferences()
    {
        OpenPreferencesCallCount++;
    }

    /// <inheritdoc />
    public void OpenRemoteDevices()
    {
        OpenRemoteDevicesCallCount++;
    }

    /// <inheritdoc />
    public void OpenReverseProxy()
    {
        OpenReverseProxyCallCount++;
    }

    /// <inheritdoc />
    public void OpenScripting()
    {
        OpenScriptingCallCount++;
    }

    /// <inheritdoc />
    public void OpenSecureSocketsLayerProxying()
    {
        OpenSecureSocketsLayerProxyingCallCount++;
    }

    /// <inheritdoc />
    public void OpenTheme()
    {
        OpenThemeCallCount++;
    }

    /// <inheritdoc />
    public void OpenThrottle()
    {
        OpenThrottleCallCount++;
    }
}
