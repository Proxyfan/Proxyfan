using System;
using System.Threading.Tasks;

namespace Proxyfan.Domain.Traffic.Tests;

/// <summary>
///     Tests for <see cref="WebSocketMessage" />.
/// </summary>
public sealed class WebSocketMessageTests
{
    /// <summary>
    ///     Verifies that the constructor stores the supplied direction, opcode, payload, and timestamp.
    /// </summary>
    [Test]
    public async Task Constructor_GivenAllFields_StoresAllValues()
    {
        var timestamp = DateTimeOffset.UtcNow;
        var payload = new byte[] { 1, 2, 3 };

        var message = new WebSocketMessage(WebSocketDirection.Outbound, WebSocketOpcode.Text, payload, timestamp);

        await Assert.That(message.Direction).IsEqualTo(WebSocketDirection.Outbound);
        await Assert.That(message.Opcode).IsEqualTo(WebSocketOpcode.Text);
        await Assert.That(message.Payload.Length).IsEqualTo(3);
        await Assert.That(message.Timestamp).IsEqualTo(timestamp);
    }
}
