namespace Proxyfan.Domain.RemoteDevices;

/// <summary>
///     The categorisation of a connected remote device, derived primarily from the
///     User-Agent header observed on its first request.
/// </summary>
public enum RemoteDeviceKind
{
    /// <summary>
    ///     The device kind could not be determined from available signals.
    /// </summary>
    Unknown = 0,

    /// <summary>
    ///     An iPhone, iPad, or iPod running iOS or iPadOS.
    /// </summary>
    Ios = 1,

    /// <summary>
    ///     An Android phone or tablet.
    /// </summary>
    Android = 2,

    /// <summary>
    ///     A Windows desktop or laptop.
    /// </summary>
    Windows = 3,

    /// <summary>
    ///     A Mac running macOS.
    /// </summary>
    MacOs = 4,

    /// <summary>
    ///     A Linux desktop or server.
    /// </summary>
    Linux = 5,
}
