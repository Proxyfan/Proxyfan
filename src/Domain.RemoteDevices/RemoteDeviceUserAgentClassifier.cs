namespace Proxyfan.Domain.RemoteDevices;

/// <summary>
///     Static helper for classifying remote devices from their User-Agent string.
/// </summary>
public static class RemoteDeviceUserAgentClassifier
{
    /// <summary>
    ///     Returns the most likely <see cref="RemoteDeviceKind" /> implied by the
    ///     <paramref name="userAgent" /> string, or <see cref="RemoteDeviceKind.Unknown" />
    ///     when no marker matches.
    /// </summary>
    /// <param name="userAgent">The User-Agent header value. May be null or empty.</param>
    /// <returns>The classified device kind.</returns>
    public static RemoteDeviceKind Classify(string? userAgent)
    {
        if (string.IsNullOrWhiteSpace(userAgent))
        {
            return RemoteDeviceKind.Unknown;
        }

        var lower = userAgent.ToLowerInvariant();
        if (lower.Contains("iphone") || lower.Contains("ipad") || lower.Contains("ipod"))
        {
            return RemoteDeviceKind.Ios;
        }

        if (lower.Contains("android"))
        {
            return RemoteDeviceKind.Android;
        }

        if (lower.Contains("windows nt") || lower.Contains("windows"))
        {
            return RemoteDeviceKind.Windows;
        }

        if (lower.Contains("macintosh") || lower.Contains("mac os x"))
        {
            return RemoteDeviceKind.MacOs;
        }

        if (lower.Contains("linux") || lower.Contains("x11"))
        {
            return RemoteDeviceKind.Linux;
        }

        return RemoteDeviceKind.Unknown;
    }
}
