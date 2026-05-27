namespace Proxyfan.Domain.RemoteDevices;

/// <summary>
///     The live status of a connected remote device.
/// </summary>
public enum RemoteDeviceStatus
{
    /// <summary>
    ///     The device has issued a request within the active window.
    /// </summary>
    Active = 0,

    /// <summary>
    ///     The device's last request is older than the idle threshold.
    /// </summary>
    Idle = 1,

    /// <summary>
    ///     The device has been disconnected (either by the user or by the idle timeout).
    /// </summary>
    Disconnected = 2,
}
