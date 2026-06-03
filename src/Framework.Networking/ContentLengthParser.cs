using System;
using System.Globalization;

namespace Proxyfan.Framework.Networking;

/// <summary>
///     Parses and validates HTTP <c>Content-Length</c> header values per RFC 7230 § 3.3.2.
///     A header value may appear as a single integer, as a comma-separated list of integers, or
///     as repeated header lines. All forms must agree on a single non-negative integer; any
///     other shape (malformed tokens, signed values, conflicting duplicates, empty lists) is
///     rejected so that callers can refuse the message instead of treating it as
///     close-delimited.
/// </summary>
public static class ContentLengthParser
{
    /// <summary>
    ///     Attempts to parse and reconcile one or more raw <c>Content-Length</c> header values
    ///     into a single non-negative length.
    /// </summary>
    /// <param name="rawValues">
    ///     The raw header values for <c>Content-Length</c>, as returned by
    ///     <see cref="Proxyfan.Domain.Traffic.HeaderCollection.GetAll" />. Must contain at
    ///     least one entry.
    /// </param>
    /// <param name="contentLength">
    ///     The reconciled non-negative content length when parsing succeeds; otherwise zero.
    /// </param>
    /// <returns>
    ///     <see langword="true" /> when every supplied value is a non-negative integer and they
    ///     all agree on the same numeric length; otherwise <see langword="false" />.
    /// </returns>
    public static bool HasValidContentLength(string[] rawValues, out long contentLength)
    {
        contentLength = 0;

        if (rawValues.Length == 0)
        {
            return false;
        }

        long? agreed = null;

        foreach (var rawValue in rawValues)
        {
            if (!HasReconciledValue(rawValue, agreed, out var updated))
            {
                return false;
            }

            agreed = updated;
        }

        if (agreed is null)
        {
            return false;
        }

        contentLength = agreed.Value;
        return true;
    }

    private static bool HasOnlyDigits(string value)
    {
        if (value.Length == 0)
        {
            return false;
        }

        foreach (var character in value)
        {
            if (character is < '0' or > '9')
            {
                return false;
            }
        }

        return true;
    }

    private static bool HasReconciledValue(string? rawValue, long? currentAgreed, out long? updatedAgreed)
    {
        updatedAgreed = currentAgreed;

        if (rawValue is null)
        {
            return false;
        }

        var tokens = rawValue.Split(',', StringSplitOptions.None);

        foreach (var token in tokens)
        {
            var trimmed = token.Trim();

            if (!HasOnlyDigits(trimmed))
            {
                return false;
            }

            if (!long.TryParse(trimmed, NumberStyles.None, CultureInfo.InvariantCulture, out var parsed))
            {
                return false;
            }

            if (updatedAgreed is null)
            {
                updatedAgreed = parsed;
            }
            else if (updatedAgreed.Value != parsed)
            {
                return false;
            }
        }

        return true;
    }
}
