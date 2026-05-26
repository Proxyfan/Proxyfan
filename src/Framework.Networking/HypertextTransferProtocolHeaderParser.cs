using Proxyfan.Domain.Traffic;
using System;

namespace Proxyfan.Framework.Networking;

/// <summary>
///     Parses raw HTTP header lines into a <see cref="HeaderCollection" />.
/// </summary>
public static class HypertextTransferProtocolHeaderParser
{
    /// <summary>
    ///     Parses the header section text that appears after the start line and before the
    ///     terminating blank line.
    /// </summary>
    /// <param name="headerSection">
    ///     The HTTP header section text.
    /// </param>
    /// <returns>
    ///     A <see cref="HeaderCollection" /> containing all valid parsed headers.
    /// </returns>
    public static HeaderCollection Parse(string headerSection)
    {
        if (string.IsNullOrEmpty(headerSection))
        {
            return HeaderCollection.Empty;
        }

        var headerLines = headerSection.Split(["\r\n"], StringSplitOptions.None);
        var currentHeaders = HeaderCollection.Empty;

        foreach (var headerLine in headerLines)
        {
            currentHeaders = AddHeaderIfValid(currentHeaders, headerLine);
        }

        return currentHeaders;
    }

    private static HeaderCollection AddHeaderIfValid(HeaderCollection headers, string headerLine)
    {
        if (string.IsNullOrEmpty(headerLine))
        {
            return headers;
        }

        var separatorIndex = headerLine.IndexOf(':', StringComparison.Ordinal);

        if (separatorIndex <= 0)
        {
            return headers;
        }

        var name = headerLine[..separatorIndex].Trim();
        var value = headerLine[(separatorIndex + 1)..].Trim();

        if (string.IsNullOrWhiteSpace(name))
        {
            return headers;
        }

        return headers.Add(name, value);
    }
}