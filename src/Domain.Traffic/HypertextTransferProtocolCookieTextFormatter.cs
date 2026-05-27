using System.Collections.Generic;
using System.Text;

namespace Proxyfan.Domain.Traffic;

/// <summary>
///     Static formatter that renders a collection of
///     <see cref="HypertextTransferProtocolCookie" /> instances as a human-readable text table
///     suitable for the Cookies inspector tab in the UI.
/// </summary>
public static class HypertextTransferProtocolCookieTextFormatter
{
    /// <summary>
    ///     Renders the cookies as a fixed-width text table. Returns an empty string when the
    ///     input is null or empty.
    /// </summary>
    /// <param name="cookies">The cookies to format.</param>
    /// <returns>The formatted table.</returns>
    public static string Format(IReadOnlyList<HypertextTransferProtocolCookie>? cookies)
    {
        if (cookies is null || cookies.Count == 0)
        {
            return string.Empty;
        }

        var widths = ComputeColumnWidths(cookies);
        var builder = new StringBuilder();
        AppendHeader(builder, widths);
        AppendSeparator(builder, widths);

        foreach (var cookie in cookies)
        {
            AppendCookieRow(builder, cookie, widths);
        }

        return builder.ToString();
    }

    private static void AppendCookieRow(StringBuilder builder, HypertextTransferProtocolCookie cookie, CookieColumnWidths widths)
    {
        builder.Append(cookie.Name.PadRight(widths.Name));
        builder.Append("  ");
        builder.Append(cookie.Value.PadRight(widths.Value));
        builder.Append("  ");
        builder.Append((cookie.Domain ?? string.Empty).PadRight(widths.Domain));
        builder.Append("  ");
        builder.Append((cookie.Path ?? string.Empty).PadRight(widths.Path));
        builder.Append("  ");
        builder.Append(FormatFlags(cookie));
        builder.AppendLine();
    }

    private static void AppendHeader(StringBuilder builder, CookieColumnWidths widths)
    {
        builder.Append("Name".PadRight(widths.Name));
        builder.Append("  ");
        builder.Append("Value".PadRight(widths.Value));
        builder.Append("  ");
        builder.Append("Domain".PadRight(widths.Domain));
        builder.Append("  ");
        builder.Append("Path".PadRight(widths.Path));
        builder.Append("  ");
        builder.Append("Flags");
        builder.AppendLine();
    }

    private static void AppendSeparator(StringBuilder builder, CookieColumnWidths widths)
    {
        var nameSeparator = new string('-', widths.Name);
        var valueSeparator = new string('-', widths.Value);
        var domainSeparator = new string('-', widths.Domain);
        var pathSeparator = new string('-', widths.Path);
        builder.Append(nameSeparator);
        builder.Append("  ");
        builder.Append(valueSeparator);
        builder.Append("  ");
        builder.Append(domainSeparator);
        builder.Append("  ");
        builder.Append(pathSeparator);
        builder.Append("  ");
        builder.Append("-----");
        builder.AppendLine();
    }

    private static CookieColumnWidths ComputeColumnWidths(IReadOnlyList<HypertextTransferProtocolCookie> cookies)
    {
        var widths = new CookieColumnWidths
        {
            Name = ComputeWidth(cookies, GetName, "Name".Length),
            Value = ComputeWidth(cookies, GetValue, "Value".Length),
            Domain = ComputeWidth(cookies, GetDomain, "Domain".Length),
            Path = ComputeWidth(cookies, GetPath, "Path".Length),
        };
        return widths;
    }

    private static int ComputeWidth(IReadOnlyList<HypertextTransferProtocolCookie> cookies, CookieColumnSelector selector, int minimum)
    {
        var width = minimum;

        foreach (var cookie in cookies)
        {
            var length = selector(cookie).Length;

            if (length > width)
            {
                width = length;
            }
        }

        return width;
    }

    private static string FormatFlags(HypertextTransferProtocolCookie cookie)
    {
        var builder = new StringBuilder();

        if (cookie.IsSecure)
        {
            builder.Append("Secure ");
        }

        if (cookie.IsHypertextTransferProtocolOnly)
        {
            builder.Append("HttpOnly ");
        }

        if (cookie.SameSite is not null)
        {
            builder.Append("SameSite=");
            builder.Append(cookie.SameSite);
            builder.Append(' ');
        }

        if (cookie.Expires is not null)
        {
            builder.Append("Expires=");
            builder.Append(cookie.Expires);
        }

        return builder.ToString().TrimEnd();
    }

    private static string GetDomain(HypertextTransferProtocolCookie cookie)
    {
        return cookie.Domain ?? string.Empty;
    }

    private static string GetName(HypertextTransferProtocolCookie cookie)
    {
        return cookie.Name;
    }

    private static string GetPath(HypertextTransferProtocolCookie cookie)
    {
        return cookie.Path ?? string.Empty;
    }

    private static string GetValue(HypertextTransferProtocolCookie cookie)
    {
        return cookie.Value;
    }
}
