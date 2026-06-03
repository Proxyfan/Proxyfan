using System;

namespace Proxyfan.Domain.Rules.Rules;

/// <summary>
///     Specifies the new destination components for a <see cref="MapRemoteRule" />. Empty fields
///     mean "preserve the original component."
/// </summary>
public sealed class MapRemoteDestination
{
    /// <summary>
    ///     Gets the destination host, or <see langword="null" /> to keep the original host.
    /// </summary>
    public string? Host { get; }

    /// <summary>
    ///     Gets a value indicating whether the original Host request header should be preserved
    ///     after URL rewriting.
    /// </summary>
    public bool IsPreservingHostHeader { get; }

    /// <summary>
    ///     Gets the destination path, or <see langword="null" /> to keep the original path.
    /// </summary>
    public string? Path { get; }

    /// <summary>
    ///     Gets the destination port, or <see langword="null" /> to keep the original port.
    /// </summary>
    public int? Port { get; }

    /// <summary>
    ///     Gets the destination scheme, or <see langword="null" /> to keep the original scheme.
    /// </summary>
    public string? Scheme { get; }

    /// <summary>
    ///     Initializes a new <see cref="MapRemoteDestination" />.
    /// </summary>
    /// <param name="scheme">The destination scheme, or null.</param>
    /// <param name="host">The destination host, or null.</param>
    /// <param name="port">The destination port, or null.</param>
    /// <param name="path">The destination path, or null.</param>
    /// <param name="isPreservingHostHeader">Whether to preserve the original Host header.</param>
    public MapRemoteDestination(string? scheme, string? host, int? port, string? path, bool isPreservingHostHeader)
    {
        scheme = string.IsNullOrWhiteSpace(scheme) ? null : scheme;
        host = string.IsNullOrWhiteSpace(host) ? null : host;
        path = string.IsNullOrWhiteSpace(path) ? null : path;

        if (scheme is not null && !Uri.CheckSchemeName(scheme))
        {
            throw new ArgumentException("Scheme must be URI-safe when provided.", nameof(scheme));
        }

        if (host is not null && Uri.CheckHostName(host) is UriHostNameType.Unknown)
        {
            throw new ArgumentException("Host must be URI-safe when provided.", nameof(host));
        }

        if (port is < 1 or > 65535)
        {
            throw new ArgumentOutOfRangeException(nameof(port), port, "Port must be between 1 and 65535.");
        }

        Scheme = scheme;
        Host = host;
        Port = port;
        Path = path;
        IsPreservingHostHeader = isPreservingHostHeader;
    }
}
