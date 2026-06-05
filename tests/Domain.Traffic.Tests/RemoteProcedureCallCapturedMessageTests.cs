using System;
using System.Threading.Tasks;

namespace Proxyfan.Domain.Traffic.Tests;

/// <summary>
///     Tests for <see cref="RemoteProcedureCallCapturedMessage" />.
/// </summary>
public sealed class RemoteProcedureCallCapturedMessageTests
{
    /// <summary>
    ///     Verifies that the constructor snapshots the payload so later caller mutations do not
    ///     change the captured bytes.
    /// </summary>
    [Test]
    public async Task Constructor_WhenSourceBufferChanges_PreservesCapturedPayload()
    {
        var payload = new byte[] { 0x01, 0x02, 0x03 };
        var message = new RemoteProcedureCallCapturedMessage(
            RemoteProcedureCallDirection.Outbound,
            false,
            payload,
            DateTimeOffset.UtcNow);

        payload[0] = 0xFF;
        payload[1] = 0xEE;
        payload[2] = 0xDD;

        await Assert.That(message.Payload.ToArray()).IsEquivalentTo(new byte[] { 0x01, 0x02, 0x03 });
    }
}
