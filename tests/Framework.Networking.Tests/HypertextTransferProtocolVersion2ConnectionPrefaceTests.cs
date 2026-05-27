using System;
using System.Threading.Tasks;
using Proxyfan.Framework.Networking;
using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.Core;

namespace Proxyfan.Framework.Networking.Tests;

public sealed class HypertextTransferProtocolVersion2ConnectionPrefaceTests
{
    [Test]
    public async Task Length_PrefaceProperty_AlwaysReturnsTwentyFour()
    {
        await Assert.That(HypertextTransferProtocolVersion2ConnectionPreface.Length).IsEqualTo(24);
    }

    [Test]
    public async Task ToArray_NoArguments_ReturnsRfcMagicString()
    {
        var bytes = HypertextTransferProtocolVersion2ConnectionPreface.ToArray();

        var expected = System.Text.Encoding.ASCII.GetBytes("PRI * HTTP/2.0\r\n\r\nSM\r\n\r\n");
        await Assert.That(bytes).IsEquivalentTo(expected);
    }

    [Test]
    public async Task ToArray_CalledTwice_ReturnsIndependentCopies()
    {
        var first = HypertextTransferProtocolVersion2ConnectionPreface.ToArray();
        var second = HypertextTransferProtocolVersion2ConnectionPreface.ToArray();
        first[0] = 0xFF;

        await Assert.That(second[0]).IsEqualTo((byte)'P');
    }

    [Test]
    public async Task HasPreface_BufferShorterThanPreface_ReturnsFalse()
    {
        var buffer = new byte[10];

        await Assert.That(HypertextTransferProtocolVersion2ConnectionPreface.HasPreface(buffer)).IsFalse();
    }

    [Test]
    public async Task HasPreface_BufferStartsWithMagicString_ReturnsTrue()
    {
        var buffer = HypertextTransferProtocolVersion2ConnectionPreface.ToArray();

        await Assert.That(HypertextTransferProtocolVersion2ConnectionPreface.HasPreface(buffer)).IsTrue();
    }

    [Test]
    public async Task HasPreface_BufferStartsWithMagicStringFollowedByTrailingBytes_ReturnsTrue()
    {
        var preface = HypertextTransferProtocolVersion2ConnectionPreface.ToArray();
        var buffer = new byte[preface.Length + 4];
        preface.AsSpan().CopyTo(buffer);
        buffer[preface.Length] = 0xAA;

        await Assert.That(HypertextTransferProtocolVersion2ConnectionPreface.HasPreface(buffer)).IsTrue();
    }

    [Test]
    public async Task HasPreface_BufferOfSameLengthButDifferentBytes_ReturnsFalse()
    {
        var buffer = new byte[24];
        for (var i = 0; i < buffer.Length; i++)
        {
            buffer[i] = 0x20;
        }

        await Assert.That(HypertextTransferProtocolVersion2ConnectionPreface.HasPreface(buffer)).IsFalse();
    }
}
