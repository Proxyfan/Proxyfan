using Proxyfan.Domain.Traffic;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.Json;

namespace Proxyfan.Client.Inspector;

/// <summary>
///     Formats the request <c>Authorization</c> header as a human-readable view, decoding
///     well-known authentication schemes (Basic, Bearer JSON-Web-Token, Digest) and falling
///     back to a raw-value display for unknown schemes.
/// </summary>
public static class AuthorizationInspectorFormatter
{
    private static readonly string[] DigestParameterOrder;
    private static readonly JsonSerializerOptions JsonOptions;

    static AuthorizationInspectorFormatter()
    {
        DigestParameterOrder =
        [
            "username",
            "realm",
            "nonce",
            "uri",
            "algorithm",
            "qop",
            "nc",
            "cnonce",
            "response",
            "opaque",
        ];

        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
        };
        JsonOptions = options;
    }

    /// <summary>
    ///     Formats the <c>Authorization</c> header from the request as a human-readable view.
    ///     Returns <see cref="string.Empty" /> when the request is <see langword="null" /> or has
    ///     no Authorization header.
    /// </summary>
    /// <param name="request">The captured HTTP request (may be <see langword="null" />).</param>
    /// <returns>The formatted Authorization view text.</returns>
    public static string Format(HypertextTransferProtocolRequestData? request)
    {
        if (request is null)
        {
            return string.Empty;
        }

        var authorization = request.Headers.Get("Authorization");
        if (string.IsNullOrWhiteSpace(authorization))
        {
            return string.Empty;
        }

        var parsed = SplitScheme(authorization);
        if (string.Equals(parsed.Scheme, "Basic", StringComparison.OrdinalIgnoreCase))
        {
            return FormatBasic(parsed.Parameter);
        }

        if (string.Equals(parsed.Scheme, "Bearer", StringComparison.OrdinalIgnoreCase))
        {
            return FormatBearer(parsed.Parameter);
        }

        if (string.Equals(parsed.Scheme, "Digest", StringComparison.OrdinalIgnoreCase))
        {
            return FormatDigest(parsed.Parameter);
        }

        return FormatUnknown(parsed.Scheme, parsed.Parameter);
    }

    private static string DecodeBase64Url(string segment)
    {
        var normalized = segment.Replace('-', '+').Replace('_', '/');
        var padded = PadBase64(normalized);
        if (padded.Length == 0)
        {
            return string.Empty;
        }

        try
        {
            var bytes = Convert.FromBase64String(padded);
            return Encoding.UTF8.GetString(bytes);
        }
        catch (FormatException)
        {
            return string.Empty;
        }
    }

    private static string FormatBasic(string parameter)
    {
        var builder = new StringBuilder();
        builder.Append("Scheme: Basic");
        builder.Append('\n');

        byte[] bytes;
        try
        {
            bytes = Convert.FromBase64String(parameter);
        }
        catch (FormatException)
        {
            builder.Append("Invalid Base64 payload.");
            return builder.ToString();
        }

        var decoded = Encoding.UTF8.GetString(bytes);
        var colonIndex = decoded.IndexOf(':', StringComparison.Ordinal);
        if (colonIndex < 0)
        {
            builder.Append(CultureInfo.InvariantCulture, $"Decoded: {decoded}");
            return builder.ToString();
        }

        var username = decoded[..colonIndex];
        var password = decoded[(colonIndex + 1)..];
        builder.Append(CultureInfo.InvariantCulture, $"Username: {username}");
        builder.Append('\n');
        builder.Append(CultureInfo.InvariantCulture, $"Password: {password}");
        return builder.ToString();
    }

    private static string FormatBearer(string parameter)
    {
        var builder = new StringBuilder();
        builder.Append("Scheme: Bearer");
        builder.Append('\n');
        builder.Append(CultureInfo.InvariantCulture, $"Token: {parameter}");

        var segments = parameter.Split('.');
        if (segments.Length != 3)
        {
            return builder.ToString();
        }

        var header = DecodeBase64Url(segments[0]);
        var payload = DecodeBase64Url(segments[1]);
        if (header.Length == 0 || payload.Length == 0)
        {
            return builder.ToString();
        }

        builder.Append('\n');
        builder.Append('\n');
        builder.Append("JSON Web Token:");
        builder.Append('\n');
        builder.Append("Header:");
        builder.Append('\n');
        builder.Append(PrettyPrintJson(header));
        builder.Append('\n');
        builder.Append('\n');
        builder.Append("Payload:");
        builder.Append('\n');
        builder.Append(PrettyPrintJson(payload));
        return builder.ToString();
    }

    private static string FormatDigest(string parameter)
    {
        var builder = new StringBuilder();
        builder.Append("Scheme: Digest");
        var parameters = ParseDigestParameters(parameter);
        foreach (var key in DigestParameterOrder)
        {
            if (parameters.TryGetValue(key, out var value))
            {
                builder.Append('\n');
                builder.Append(CultureInfo.InvariantCulture, $"{key}: {value}");
            }
        }

        foreach (var pair in parameters)
        {
            if (!HasKnownDigestKey(pair.Key))
            {
                builder.Append('\n');
                builder.Append(CultureInfo.InvariantCulture, $"{pair.Key}: {pair.Value}");
            }
        }

        return builder.ToString();
    }

    private static string FormatUnknown(string scheme, string parameter)
    {
        var builder = new StringBuilder();
        var displayScheme = string.IsNullOrEmpty(scheme) ? "(none)" : scheme;
        builder.Append(CultureInfo.InvariantCulture, $"Scheme: {displayScheme}");
        builder.Append('\n');
        builder.Append(CultureInfo.InvariantCulture, $"Value: {parameter}");
        return builder.ToString();
    }

    private static bool HasKnownDigestKey(string key)
    {
        foreach (var known in DigestParameterOrder)
        {
            if (string.Equals(known, key, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static string PadBase64(string segment)
    {
        return (segment.Length % 4) switch
        {
            0 => segment,
            2 => segment + "==",
            3 => segment + "=",
            _ => string.Empty,
        };
    }

    private static Dictionary<string, string> ParseDigestParameters(string parameter)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var index = 0;
        while (index < parameter.Length)
        {
            index = SkipSeparators(parameter, index);
            if (index >= parameter.Length)
            {
                break;
            }

            var keyResult = ReadDigestKey(parameter, index);
            if (keyResult.NextIndex >= parameter.Length)
            {
                break;
            }

            var valueResult = ReadDigestValue(parameter, keyResult.NextIndex + 1);
            result[keyResult.Key] = valueResult.Value;
            index = valueResult.NextIndex;
        }

        return result;
    }

    private static string PrettyPrintJson(string json)
    {
        try
        {
            using var document = JsonDocument.Parse(json);
            return JsonSerializer.Serialize(document.RootElement, JsonOptions);
        }
        catch (JsonException)
        {
            return json;
        }
    }

    private static DigestValueReadResult ReadBareDigestValue(string parameter, int start)
    {
        var index = start;
        while (index < parameter.Length && parameter[index] != ',')
        {
            index++;
        }

        var value = parameter[start..index].Trim();
        return new DigestValueReadResult(value, index);
    }

    private static DigestKeyReadResult ReadDigestKey(string parameter, int start)
    {
        var index = start;
        while (index < parameter.Length && parameter[index] != '=')
        {
            index++;
        }

        var key = parameter[start..index].Trim();
        return new DigestKeyReadResult(key, index);
    }

    private static DigestValueReadResult ReadDigestValue(string parameter, int start)
    {
        if (start < parameter.Length && parameter[start] == '"')
        {
            return ReadQuotedDigestValue(parameter, start + 1);
        }

        return ReadBareDigestValue(parameter, start);
    }

    private static DigestValueReadResult ReadQuotedDigestValue(string parameter, int start)
    {
        var index = start;
        while (index < parameter.Length && parameter[index] != '"')
        {
            if (parameter[index] == '\\' && index + 1 < parameter.Length)
            {
                index += 2;
                continue;
            }

            index++;
        }

        var value = parameter[start..index];
        var nextIndex = index < parameter.Length ? index + 1 : index;
        return new DigestValueReadResult(value, nextIndex);
    }

    private static int SkipSeparators(string parameter, int start)
    {
        var index = start;
        while (index < parameter.Length && (parameter[index] == ',' || char.IsWhiteSpace(parameter[index])))
        {
            index++;
        }

        return index;
    }

    private static AuthorizationHeader SplitScheme(string authorization)
    {
        var trimmed = authorization.Trim();
        var spaceIndex = trimmed.IndexOf(' ', StringComparison.Ordinal);
        if (spaceIndex < 0)
        {
            return new AuthorizationHeader(trimmed, string.Empty);
        }

        var scheme = trimmed[..spaceIndex];
        var parameter = trimmed[(spaceIndex + 1)..].Trim();
        return new AuthorizationHeader(scheme, parameter);
    }

    private readonly record struct AuthorizationHeader
    {
        public string Parameter { get; }

        public string Scheme { get; }

        public AuthorizationHeader(string scheme, string parameter)
        {
            Scheme = scheme;
            Parameter = parameter;
        }
    }

    private readonly record struct DigestKeyReadResult
    {
        public string Key { get; }

        public int NextIndex { get; }

        public DigestKeyReadResult(string key, int nextIndex)
        {
            Key = key;
            NextIndex = nextIndex;
        }
    }

    private readonly record struct DigestValueReadResult
    {
        public int NextIndex { get; }

        public string Value { get; }

        public DigestValueReadResult(string value, int nextIndex)
        {
            Value = value;
            NextIndex = nextIndex;
        }
    }
}
