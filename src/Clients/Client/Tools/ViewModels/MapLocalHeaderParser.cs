using System.Collections.Generic;

namespace Proxyfan.Client.Tools.ViewModels;

/// <summary>
///     Parses a multi-line "Name: Value" string into a list of header name/value pairs.
///     Blank lines and lines without a colon separator are ignored.
/// </summary>
public static class MapLocalHeaderParser
{
    /// <summary>
    ///     Parses the supplied multi-line text into a list of header name/value pairs.
    /// </summary>
    /// <param name="text">The multi-line text to parse.</param>
    /// <returns>The parsed list of header name/value pairs.</returns>
    public static List<KeyValuePair<string, string>> Parse(string text)
    {
        var result = new List<KeyValuePair<string, string>>();
        if (string.IsNullOrWhiteSpace(text))
        {
            return result;
        }

        var lines = text.Split('\n');
        foreach (var rawLine in lines)
        {
            var line = rawLine.Trim();
            if (line.Length == 0)
            {
                continue;
            }

            var separator = line.IndexOf(':');
            if (separator <= 0)
            {
                continue;
            }

            var name = line[..separator].Trim();
            var value = line[(separator + 1)..].Trim();
            if (name.Length == 0)
            {
                continue;
            }

            var pair = new KeyValuePair<string, string>(name, value);
            result.Add(pair);
        }

        return result;
    }
}
