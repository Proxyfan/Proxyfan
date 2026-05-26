using System;

namespace Proxyfan.Domain.Proxy;

/// <summary>
///     Strongly-typed configuration options for the proxy listener, bound from the <c>proxy</c>
///     section of the application configuration.
/// </summary>
public sealed class ProxyOptions
{
    /// <summary>
    ///     The configuration section key used when binding these options.
    /// </summary>
    public const string SectionKey = "proxy";

    /// <summary>
    ///     Gets or sets a value indicating whether the proxy should start automatically on application launch.
    ///     Default: <see langword="true" />.
    /// </summary>
    public bool IsAutoStart { get; set; }

    /// <summary>
    ///     Gets or sets a value indicating whether the proxy registers itself as the system proxy on Windows.
    ///     Default: <see langword="true" />.
    /// </summary>
    public bool IsRegisterSystemProxy { get; set; }

    /// <summary>
    ///     Gets or sets the maximum number of concurrent connections the listener accepts. Default: 1000.
    /// </summary>
    public int MaxConnections { get; set; }

    /// <summary>
    ///     Gets or sets the TCP port the proxy listener binds to. Valid range: 1024-65535. Default: 8080.
    /// </summary>
    public int Port { get; set; }

    /// <summary>
    ///     Gets or sets the maximum time the connection dispatcher waits for the initial
    ///     bytes needed to detect the protocol. If no data arrives within this duration
    ///     the connection is closed. A value of <see cref="TimeSpan.Zero" /> disables the
    ///     timeout. Default: 5 seconds.
    /// </summary>
    public TimeSpan ProtocolDetectionTimeout { get; set; }

    /// <summary>
    ///     Initializes a new <see cref="ProxyOptions" /> with default values.
    /// </summary>
    public ProxyOptions()
    {
        IsAutoStart = true;
        IsRegisterSystemProxy = true;
        MaxConnections = 1000;
        Port = 8080;
        ProtocolDetectionTimeout = TimeSpan.FromSeconds(5);
    }
}