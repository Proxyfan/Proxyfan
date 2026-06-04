using Proxyfan.Domain.Proxy;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Net.Sockets;

namespace Proxyfan.Framework.Networking;

/// <summary>
///     Resolves bind-address and source-address policy for the forward proxy listener.
/// </summary>
public sealed class ProxyListenerAccessPolicy
{
    private readonly List<ProxyListenerAllowedSource> _allowedSources;

    /// <summary>
    ///     Gets the listener bind address resolved from <see cref="ProxyOptions.BindAddress" />.
    /// </summary>
    public IPAddress BindAddress { get; }

    /// <summary>
    ///     Initializes a new <see cref="ProxyListenerAccessPolicy" /> from current proxy options.
    /// </summary>
    /// <param name="options">The bound proxy options.</param>
    /// <exception cref="InvalidOperationException">
    ///     Thrown when BindAddress is invalid, an allow-list entry is malformed, or a non-loopback
    ///     bind has no allow-list entries.
    /// </exception>
    public ProxyListenerAccessPolicy(ProxyOptions options)
    {
        if (!IPAddress.TryParse(options.BindAddress, out var parsedBindAddress))
        {
            throw new InvalidOperationException($"Proxy bind address '{options.BindAddress}' is invalid.");
        }

        BindAddress = parsedBindAddress;

        var configuredAllowedSources = options.AllowedRemoteSources ?? [];
        var allowedSources = new List<ProxyListenerAllowedSource>(configuredAllowedSources.Length);
        foreach (var entry in configuredAllowedSources)
        {
            allowedSources.Add(ParseAllowedSource(entry));
        }

        if (!IPAddress.IsLoopback(BindAddress) && allowedSources.Count == 0)
        {
            throw new InvalidOperationException(
                "Binding proxy listener to a non-loopback address requires at least one AllowedRemoteSources entry.");
        }

        _allowedSources = allowedSources;
    }

    /// <summary>
    ///     Returns true when the remote endpoint is allowed by the current source policy.
    /// </summary>
    /// <param name="remoteEndPoint">The candidate remote endpoint from an accepted socket.</param>
    /// <returns>
    ///     <see langword="true" /> when the endpoint is permitted by the configured allow-list;
    ///     otherwise <see langword="false" />.
    /// </returns>
    public bool CanAllowRemoteEndpoint(EndPoint remoteEndPoint)
    {
        if (IPAddress.IsLoopback(BindAddress))
        {
            return true;
        }

        if (remoteEndPoint is not IPEndPoint ipEndPoint)
        {
            return false;
        }

        var remoteAddress = NormalizeAddress(ipEndPoint.Address);

        foreach (var allowedSource in _allowedSources)
        {
            if (allowedSource.CanContainAddress(remoteAddress))
            {
                return true;
            }
        }

        return false;
    }

    private IPAddress NormalizeAddress(IPAddress address)
    {
        if (address.IsIPv4MappedToIPv6)
        {
            return address.MapToIPv4();
        }

        return address;
    }

    private ProxyListenerAllowedSource ParseAllowedSource(string entry)
    {
        var trimmed = entry.Trim();
        if (trimmed.Length == 0)
        {
            throw new InvalidOperationException("AllowedRemoteSources cannot contain empty entries.");
        }

        var slashIndex = trimmed.IndexOf('/');
        if (slashIndex < 0)
        {
            if (!IPAddress.TryParse(trimmed, out var ipAddress))
            {
                throw new InvalidOperationException($"AllowedRemoteSources entry '{entry}' is not a valid IP/CIDR.");
            }

            var normalized = NormalizeAddress(ipAddress);
            var fullPrefixLength = normalized.AddressFamily == AddressFamily.InterNetwork ? 32 : 128;
            return new ProxyListenerAllowedSource(normalized, fullPrefixLength);
        }

        var addressPart = trimmed[..slashIndex];
        var prefixPart = trimmed[(slashIndex + 1)..];
        if (!IPAddress.TryParse(addressPart, out var networkAddress))
        {
            throw new InvalidOperationException($"AllowedRemoteSources entry '{entry}' has an invalid CIDR address.");
        }

        if (!int.TryParse(prefixPart, NumberStyles.None, CultureInfo.InvariantCulture, out var prefixLength))
        {
            throw new InvalidOperationException($"AllowedRemoteSources entry '{entry}' has an invalid CIDR prefix.");
        }

        var normalizedAddress = NormalizeAddress(networkAddress);
        var maxPrefixLength = normalizedAddress.AddressFamily == AddressFamily.InterNetwork ? 32 : 128;
        if (prefixLength < 0 || prefixLength > maxPrefixLength)
        {
            throw new InvalidOperationException(
                $"AllowedRemoteSources entry '{entry}' has CIDR prefix outside [0-{maxPrefixLength.ToString(CultureInfo.InvariantCulture)}].");
        }

        return new ProxyListenerAllowedSource(normalizedAddress, prefixLength);
    }

    private readonly struct ProxyListenerAllowedSource
    {
        private readonly IPAddress _networkAddress;
        private readonly int _prefixLength;

        public ProxyListenerAllowedSource(IPAddress networkAddress, int prefixLength)
        {
            _networkAddress = networkAddress;
            _prefixLength = prefixLength;
        }

        public bool CanContainAddress(IPAddress candidate)
        {
            if (candidate.AddressFamily != _networkAddress.AddressFamily)
            {
                return false;
            }

            var networkBytes = _networkAddress.GetAddressBytes();
            var candidateBytes = candidate.GetAddressBytes();
            var fullBytes = _prefixLength / 8;
            var remainingBits = _prefixLength % 8;

            for (var byteIndex = 0; byteIndex < fullBytes; byteIndex++)
            {
                if (networkBytes[byteIndex] != candidateBytes[byteIndex])
                {
                    return false;
                }
            }

            if (remainingBits == 0)
            {
                return true;
            }

            var mask = 0xFF << (8 - remainingBits);
            return (networkBytes[fullBytes] & mask) == (candidateBytes[fullBytes] & mask);
        }
    }
}
