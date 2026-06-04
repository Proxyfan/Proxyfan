using Microsoft.Extensions.Options;
using System;
using System.Net;

namespace Proxyfan.Domain.Proxy;

/// <summary>
///     Validates <see cref="ProxyOptions" /> at application startup to surface configuration
///     errors before the proxy attempts to bind.
/// </summary>
public sealed class ProxyOptionsValidator : IValidateOptions<ProxyOptions>
{
    private const int MaxPort = 65535;
    private const int MinPort = 1024;

    /// <inheritdoc />
    public ValidateOptionsResult Validate(string? name, ProxyOptions options)
    {
        _ = name;

        if (!IPAddress.TryParse(options.BindAddress, out var bindAddress))
        {
            return ValidateOptionsResult.Fail(
                $"BindAddress '{options.BindAddress}' is not a valid IP address.");
        }

        var allowedRemoteSources = options.AllowedRemoteSources ?? [];
        if (!IPAddress.IsLoopback(bindAddress) && allowedRemoteSources.Length == 0)
        {
            return ValidateOptionsResult.Fail(
                "A non-loopback BindAddress requires at least one AllowedRemoteSources entry.");
        }

        if (options.Port is < MinPort or > MaxPort)
        {
            return ValidateOptionsResult.Fail(
                $"Proxy port {options.Port} is out of the valid range [{MinPort}–{MaxPort}].");
        }

        if (options.MaxConnections is <= 0)
        {
            return ValidateOptionsResult.Fail(
                $"MaxConnections must be greater than zero, but was {options.MaxConnections}.");
        }

        if (options.ProtocolDetectionTimeout < TimeSpan.Zero)
        {
            return ValidateOptionsResult.Fail(
                $"ProtocolDetectionTimeout must be zero or positive, but was {options.ProtocolDetectionTimeout}.");
        }

        return ValidateOptionsResult.Success;
    }
}
