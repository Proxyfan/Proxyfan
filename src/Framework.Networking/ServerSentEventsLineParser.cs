using System;

namespace Proxyfan.Framework.Networking;

/// <summary>
///     Static helpers used by <see cref="ServerSentEventsParser" /> for line-level operations.
/// </summary>
public static class ServerSentEventsLineParser
{
    /// <summary>
    ///     Parses a single SSE field line into a <see cref="ServerSentEventField" />, or
    ///     <see langword="null" /> when the line is empty, a comment, or malformed.
    /// </summary>
    /// <param name="line">The line to parse.</param>
    /// <returns>The parsed field, or null.</returns>
    public static ServerSentEventField? ParseField(string line)
    {
        if (line.Length == 0 || line[0] == ':')
        {
            return null;
        }

        var separatorIndex = line.IndexOf(':', StringComparison.Ordinal);

        if (separatorIndex < 0)
        {
            var fieldOnly = new ServerSentEventField(line, string.Empty);
            return fieldOnly;
        }

        var name = line[..separatorIndex];
        var rawValue = line[(separatorIndex + 1)..];
        var value = rawValue.Length > 0 && rawValue[0] == ' ' ? rawValue[1..] : rawValue;
        var field = new ServerSentEventField(name, value);
        return field;
    }
}
