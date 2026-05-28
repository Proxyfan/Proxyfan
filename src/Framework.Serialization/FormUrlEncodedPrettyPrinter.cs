using System;
using System.Net;
using System.Text;

namespace Proxyfan.Framework.Serialization;

/// <summary>
///     Pretty-prints <c>application/x-www-form-urlencoded</c> bodies as a key/value list.
///     Each key/value pair is rendered on its own line as <c>key: value</c> with both
///     components URL-decoded. Returns the original text when the body is empty.
/// </summary>
public static class FormUrlEncodedPrettyPrinter
{
    /// <summary>
    ///     Pretty-prints a form-urlencoded body.
    /// </summary>
    /// <param name="rawForm">The raw body text.</param>
    /// <returns>The formatted text, or the original input when empty.</returns>
    public static string PrettyPrint(string rawForm)
    {
        if (string.IsNullOrEmpty(rawForm))
        {
            return rawForm;
        }

        var builder = new StringBuilder();
        var pairs = rawForm.Split('&');

        for (var index = 0; index < pairs.Length; index++)
        {
            var pair = pairs[index];

            if (pair.Length == 0)
            {
                continue;
            }

            AppendPair(builder, pair);

            if (index < pairs.Length - 1)
            {
                builder.Append('\n');
            }
        }

        return builder.ToString();
    }

    private static void AppendPair(StringBuilder builder, string pair)
    {
        var separatorIndex = pair.IndexOf('=', StringComparison.Ordinal);

        if (separatorIndex < 0)
        {
            builder.Append(Decode(pair));
            builder.Append(':');
            return;
        }

        var key = pair[..separatorIndex];
        var value = pair[(separatorIndex + 1)..];
        builder.Append(Decode(key));
        builder.Append(": ");
        builder.Append(Decode(value));
    }

    private static string Decode(string value)
    {
        try
        {
            return WebUtility.UrlDecode(value.Replace('+', ' '));
        }
        catch (ArgumentException)
        {
            return value;
        }
    }
}
