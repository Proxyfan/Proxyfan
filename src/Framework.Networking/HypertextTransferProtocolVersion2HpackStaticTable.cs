using System;
using System.Collections.Generic;

namespace Proxyfan.Framework.Networking;

/// <summary>
///     RFC 7541 § 2.3.1 — the immutable static table of 61 well-known HTTP header
///     name and value pairs used by HPACK for indexed representations. HPACK
///     entries are 1-indexed: index 1 is <c>:authority</c>; index 61 is
///     <c>www-authenticate</c>.
/// </summary>
public static class HypertextTransferProtocolVersion2HpackStaticTable
{
    private static readonly string[] Names;
    private static readonly string[] Values;

    /// <summary>
    ///     Gets the number of entries in the static table.
    /// </summary>
    public static int Count => Names.Length;

    static HypertextTransferProtocolVersion2HpackStaticTable()
    {
        Names = BuildNames();
        Values = BuildValues();
    }

    /// <summary>
    ///     Locates the lowest entry whose name matches <paramref name="name" /> (case-insensitive).
    ///     If <paramref name="value" /> also matches, the result is flagged as an exact match.
    /// </summary>
    /// <param name="name">The header name to look up.</param>
    /// <param name="value">The header value to match.</param>
    /// <returns>
    ///     A lookup result whose <c>Index</c> is the 1-based static-table position of the match
    ///     (0 when not found) and whose <c>IsExactMatch</c> flag indicates whether the value
    ///     also matched.
    /// </returns>
    public static HypertextTransferProtocolVersion2HpackTableLookup Find(string name, string value)
    {
        var nameMatch = 0;
        for (var entryIndex = 0; entryIndex < Names.Length; entryIndex++)
        {
            if (!string.Equals(Names[entryIndex], name, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }
            if (string.Equals(Values[entryIndex], value, StringComparison.Ordinal))
            {
                return new HypertextTransferProtocolVersion2HpackTableLookup(entryIndex + 1, isExactMatch: true);
            }
            if (nameMatch == 0)
            {
                nameMatch = entryIndex + 1;
            }
        }
        return new HypertextTransferProtocolVersion2HpackTableLookup(nameMatch, isExactMatch: false);
    }

    /// <summary>
    ///     Returns the entry at the 1-based <paramref name="index" /> defined by RFC 7541 § 2.3.1.
    /// </summary>
    /// <param name="index">The 1-based static-table index.</param>
    /// <returns>The header field entry at the given index.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    ///     When <paramref name="index" /> is outside [1, <see cref="Count" />].
    /// </exception>
    public static HypertextTransferProtocolVersion2HpackHeaderField Get(int index)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(index, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(index, Names.Length);
        var name = Names[index - 1];
        var value = Values[index - 1];
        var field = new HypertextTransferProtocolVersion2HpackHeaderField(name, value);
        return field;
    }

    /// <summary>
    ///     Returns a fresh snapshot of all entries, exposed for tests and introspection.
    /// </summary>
    /// <returns>A list of all 61 static-table entries, in 1-based order.</returns>
    public static IReadOnlyList<HypertextTransferProtocolVersion2HpackHeaderField> Snapshot()
    {
        var snapshot = new List<HypertextTransferProtocolVersion2HpackHeaderField>(Names.Length);
        for (var index = 1; index <= Names.Length; index++)
        {
            var entry = Get(index);
            snapshot.Add(entry);
        }
        return snapshot;
    }

    private static string[] BuildNames()
    {
        var names = new string[61];
        FillPseudoHeaderNames(names);
        FillRequestHeaderNames(names);
        FillResponseHeaderNames(names);
        return names;
    }

    private static string[] BuildValues()
    {
        var values = new string[61];
        for (var index = 0; index < values.Length; index++)
        {
            values[index] = string.Empty;
        }
        values[1] = "GET";
        values[2] = "POST";
        values[3] = "/";
        values[4] = "/index.html";
        values[5] = "http";
        values[6] = "https";
        values[7] = "200";
        values[8] = "204";
        values[9] = "206";
        values[10] = "304";
        values[11] = "400";
        values[12] = "404";
        values[13] = "500";
        values[15] = "gzip, deflate";
        return values;
    }

    private static void FillPseudoHeaderNames(string[] names)
    {
        names[0] = ":authority";
        names[1] = ":method";
        names[2] = ":method";
        names[3] = ":path";
        names[4] = ":path";
        names[5] = ":scheme";
        names[6] = ":scheme";
        names[7] = ":status";
        names[8] = ":status";
        names[9] = ":status";
        names[10] = ":status";
        names[11] = ":status";
        names[12] = ":status";
        names[13] = ":status";
    }

    private static void FillRequestHeaderNames(string[] names)
    {
        names[14] = "accept-charset";
        names[15] = "accept-encoding";
        names[16] = "accept-language";
        names[17] = "accept-ranges";
        names[18] = "accept";
        names[19] = "access-control-allow-origin";
        names[20] = "age";
        names[21] = "allow";
        names[22] = "authorization";
        names[23] = "cache-control";
        names[24] = "content-disposition";
        names[25] = "content-encoding";
        names[26] = "content-language";
        names[27] = "content-length";
        names[28] = "content-location";
        names[29] = "content-range";
        names[30] = "content-type";
        names[31] = "cookie";
        names[32] = "date";
        names[33] = "etag";
        names[34] = "expect";
        names[35] = "expires";
        names[36] = "from";
    }

    private static void FillResponseHeaderNames(string[] names)
    {
        names[37] = "host";
        names[38] = "if-match";
        names[39] = "if-modified-since";
        names[40] = "if-none-match";
        names[41] = "if-range";
        names[42] = "if-unmodified-since";
        names[43] = "last-modified";
        names[44] = "link";
        names[45] = "location";
        names[46] = "max-forwards";
        names[47] = "proxy-authenticate";
        names[48] = "proxy-authorization";
        names[49] = "range";
        names[50] = "referer";
        names[51] = "refresh";
        names[52] = "retry-after";
        names[53] = "server";
        names[54] = "set-cookie";
        names[55] = "strict-transport-security";
        names[56] = "transfer-encoding";
        names[57] = "user-agent";
        names[58] = "vary";
        names[59] = "via";
        names[60] = "www-authenticate";
    }
}
