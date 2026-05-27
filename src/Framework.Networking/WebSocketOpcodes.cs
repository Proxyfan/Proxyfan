using Proxyfan.Domain.Traffic;

namespace Proxyfan.Framework.Networking;

/// <summary>
///     Static helpers for <see cref="WebSocketOpcode" /> classification (control vs. data).
/// </summary>
public static class WebSocketOpcodes
{
    /// <summary>
    ///     Returns <see langword="true" /> when the opcode identifies a control frame
    ///     (Close, Ping, or Pong).
    /// </summary>
    /// <param name="opcode">The opcode to test.</param>
    /// <returns><see langword="true" /> when the opcode is a control frame.</returns>
    public static bool HasControlBehavior(WebSocketOpcode opcode)
    {
        return opcode is WebSocketOpcode.Close or WebSocketOpcode.Ping or WebSocketOpcode.Pong;
    }

    /// <summary>
    ///     Returns <see langword="true" /> when the supplied raw value is one of the six
    ///     known RFC 6455 opcode numbers.
    /// </summary>
    /// <param name="opcodeRaw">The raw opcode byte.</param>
    /// <returns><see langword="true" /> when known.</returns>
    public static bool HasKnownValue(int opcodeRaw)
    {
        return opcodeRaw is 0x0 or 0x1 or 0x2 or 0x8 or 0x9 or 0xA;
    }
}
