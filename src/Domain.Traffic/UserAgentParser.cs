using System;

namespace Proxyfan.Domain.Traffic;

/// <summary>
///     Best-effort parser for HTTP User-Agent header values, splitting "Product/Version" into
///     <see cref="UserAgentInfo.ProductName" /> and <see cref="UserAgentInfo.ProductVersion" />.
/// </summary>
public static class UserAgentParser
{
    /// <summary>
    ///     Parses the supplied User-Agent header value.
    /// </summary>
    /// <param name="rawValue">The raw User-Agent header value.</param>
    /// <returns>The parsed information.</returns>
    public static UserAgentInfo Parse(string rawValue)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rawValue);

        var trimmed = rawValue.Trim();
        var firstSpaceIndex = trimmed.IndexOf(' ', StringComparison.Ordinal);
        string firstToken;
        if (firstSpaceIndex < 0)
        {
            firstToken = trimmed;
        }
        else
        {
            firstToken = trimmed[..firstSpaceIndex];
        }

        var slashIndex = firstToken.IndexOf('/', StringComparison.Ordinal);

        if (slashIndex < 0)
        {
            var info = new UserAgentInfo(firstToken, string.Empty, rawValue);
            return info;
        }

        var productName = firstToken[..slashIndex];
        var productVersion = firstToken[(slashIndex + 1)..];
        var parsed = new UserAgentInfo(productName, productVersion, rawValue);
        return parsed;
    }
}
