using Proxyfan.Client.Inspector;
using Proxyfan.Domain.Traffic;
using System.Threading.Tasks;

namespace Proxyfan.Client.Tests;

/// <summary>
///     Tests for <see cref="WebSocketOpcodeFormatter" /> covering the labels produced for
///     each <see cref="WebSocketOpcode" /> value.
/// </summary>
public sealed class WebSocketOpcodeFormatterTests
{
    /// <summary>
    ///     Verifies the display label for each known opcode value.
    /// </summary>
    [Test]
    [Arguments(WebSocketOpcode.Text, "Text")]
    [Arguments(WebSocketOpcode.Binary, "Binary")]
    [Arguments(WebSocketOpcode.Ping, "Ping")]
    [Arguments(WebSocketOpcode.Pong, "Pong")]
    [Arguments(WebSocketOpcode.Close, "Close")]
    public async Task FormatOpcode_KnownOpcode_ReturnsDescriptiveLabel(WebSocketOpcode opcode, string expected)
    {
        var result = WebSocketOpcodeFormatter.FormatOpcode(opcode);

        await Assert.That(result).IsEqualTo(expected);
    }

    /// <summary>
    ///     Verifies that an unknown opcode value falls back to the enum's <c>ToString</c>.
    /// </summary>
    [Test]
    public async Task FormatOpcode_UnknownOpcode_FallsBackToEnumName()
    {
        var unknown = (WebSocketOpcode)200;

        var result = WebSocketOpcodeFormatter.FormatOpcode(unknown);

        await Assert.That(result).IsEqualTo("200");
    }
}
