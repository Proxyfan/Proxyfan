using System;
using System.Net;

namespace Proxyfan.Domain.DomainNameSystemSpoofing;

/// <summary>
///     A single DNS override entry: when a request targets <see cref="Hostname" /> (case-
///     insensitive, exact match), connect to <see cref="OverrideAddress" /> instead of
///     resolving the actual DNS record.
/// </summary>
public sealed class DomainNameSystemOverrideEntry
{
    /// <summary>
    ///     Gets the host name to override (case-insensitive comparison).
    /// </summary>
    public string Hostname { get; }

    /// <summary>
    ///     Gets the IP address to connect to instead of the hostname's real DNS record.
    /// </summary>
    public IPAddress OverrideAddress { get; }

    /// <summary>
    ///     Initializes a new <see cref="DomainNameSystemOverrideEntry" />.
    /// </summary>
    /// <param name="hostname">The host name to match.</param>
    /// <param name="overrideAddress">The IP address to substitute.</param>
    /// <exception cref="ArgumentException">Thrown when hostname is null or whitespace.</exception>
    public DomainNameSystemOverrideEntry(string hostname, IPAddress overrideAddress)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(hostname);
        Hostname = hostname;
        OverrideAddress = overrideAddress;
    }
}
