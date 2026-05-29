using Proxyfan.Domain.Traffic;

namespace Proxyfan.Client.Inspector;

/// <summary>
///     Formats <see cref="WebSocketOpcode" /> values as concise display strings.
/// </summary>
public static class WebSocketOpcodeFormatter
{
    /// <summary>
    ///     Returns the display label for the supplied opcode (Text, Binary, Ping,
    ///     Pong, Close), or the raw enum name for any other value.
    /// </summary>
    /// <param name="opcode">The opcode to format.</param>
    /// <returns>A short display label.</returns>
    public static string FormatOpcode(WebSocketOpcode opcode)
    {
        if (opcode == WebSocketOpcode.Text)
        {
            return "Text";
        }

        if (opcode == WebSocketOpcode.Binary)
        {
            return "Binary";
        }

        if (opcode == WebSocketOpcode.Ping)
        {
            return "Ping";
        }

        if (opcode == WebSocketOpcode.Pong)
        {
            return "Pong";
        }

        if (opcode == WebSocketOpcode.Close)
        {
            return "Close";
        }

        return opcode.ToString();
    }
}
