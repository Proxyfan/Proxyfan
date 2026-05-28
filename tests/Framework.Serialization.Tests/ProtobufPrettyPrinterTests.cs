using System;
using System.Text;
using System.Threading.Tasks;

namespace Proxyfan.Framework.Serialization.Tests;

/// <summary>
///     Tests for <see cref="ProtobufPrettyPrinter" /> covering all wire types, nested
///     messages, UTF-8/byte heuristics, and malformed payloads.
/// </summary>
public sealed class ProtobufPrettyPrinterTests
{
    /// <summary>
    ///     Empty payload renders as empty string.
    /// </summary>
    [Test]
    public async Task PrettyPrint_EmptyPayload_ReturnsEmptyString()
    {
        var result = ProtobufPrettyPrinter.PrettyPrint(ReadOnlyMemory<byte>.Empty);

        await Assert.That(result).IsEqualTo(string.Empty);
    }

    /// <summary>
    ///     Single varint renders as a "Field N (varint): value" line.
    /// </summary>
    [Test]
    public async Task PrettyPrint_SingleVarint_RendersVarintLine()
    {
        var payload = new byte[] { 0x08, 0x96, 0x01 };

        var result = ProtobufPrettyPrinter.PrettyPrint(payload);

        await Assert.That(result).IsEqualTo("Field 1 (varint): 150");
    }

    /// <summary>
    ///     UTF-8 text inside a length-delimited field is rendered as a string.
    /// </summary>
    [Test]
    public async Task PrettyPrint_LengthDelimitedUtf8_RendersAsString()
    {
        var stringBytes = Encoding.UTF8.GetBytes("testing");
        var payload = new byte[2 + stringBytes.Length];
        payload[0] = 0x12;
        payload[1] = (byte)stringBytes.Length;
        stringBytes.CopyTo(payload, 2);

        var result = ProtobufPrettyPrinter.PrettyPrint(payload);

        await Assert.That(result).IsEqualTo("Field 2 (string): \"testing\"");
    }

    /// <summary>
    ///     A length-delimited field that itself parses as a valid nested protobuf message is
    ///     rendered as an indented sub-tree.
    /// </summary>
    [Test]
    public async Task PrettyPrint_NestedMessage_RendersIndentedSubtree()
    {
        var inner = new byte[] { 0x08, 0x2A };
        var payload = new byte[2 + inner.Length];
        payload[0] = 0x12;
        payload[1] = (byte)inner.Length;
        inner.CopyTo(payload, 2);

        var result = ProtobufPrettyPrinter.PrettyPrint(payload);

        await Assert.That(result.Contains("Field 2 (message): {")).IsTrue();
        await Assert.That(result.Contains("  Field 1 (varint): 42")).IsTrue();
        await Assert.That(result.Contains('}')).IsTrue();
    }

    /// <summary>
    ///     Non-UTF-8 binary payload that cannot be parsed as nested protobuf is rendered as
    ///     a hex byte string with a "bytes" label.
    /// </summary>
    [Test]
    public async Task PrettyPrint_BinaryLengthDelimited_RendersAsHexBytes()
    {
        var binary = new byte[] { 0xFF, 0xFE, 0xFD, 0xFC };
        var payload = new byte[2 + binary.Length];
        payload[0] = 0x12;
        payload[1] = (byte)binary.Length;
        binary.CopyTo(payload, 2);

        var result = ProtobufPrettyPrinter.PrettyPrint(payload);

        await Assert.That(result).IsEqualTo("Field 2 (bytes, 4): 0xfffefdfc");
    }

    /// <summary>
    ///     Fixed32 wire type renders as a uint.
    /// </summary>
    [Test]
    public async Task PrettyPrint_Fixed32_RendersUint()
    {
        var payload = new byte[] { 0x0D, 0x07, 0x00, 0x00, 0x00 };

        var result = ProtobufPrettyPrinter.PrettyPrint(payload);

        await Assert.That(result).IsEqualTo("Field 1 (fixed32): 7");
    }

    /// <summary>
    ///     Fixed64 wire type renders as a ulong.
    /// </summary>
    [Test]
    public async Task PrettyPrint_Fixed64_RendersUlong()
    {
        var payload = new byte[] { 0x09, 0x05, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };

        var result = ProtobufPrettyPrinter.PrettyPrint(payload);

        await Assert.That(result).IsEqualTo("Field 1 (fixed64): 5");
    }

    /// <summary>
    ///     Truncated varint (no terminator byte) falls back to hex rendering instead of
    ///     throwing.
    /// </summary>
    [Test]
    public async Task PrettyPrint_TruncatedVarint_FallsBackToHex()
    {
        var payload = new byte[] { 0x08, 0x96 };

        var result = ProtobufPrettyPrinter.PrettyPrint(payload);

        await Assert.That(result).IsEqualTo("0896");
    }

    /// <summary>
    ///     Multiple top-level fields each render on their own line, in encounter order.
    /// </summary>
    [Test]
    public async Task PrettyPrint_MultipleFields_RendersOneLineEach()
    {
        var stringBytes = Encoding.UTF8.GetBytes("hi");
        var payload = new byte[] { 0x08, 0x2A, 0x12, (byte)stringBytes.Length, stringBytes[0], stringBytes[1] };

        var result = ProtobufPrettyPrinter.PrettyPrint(payload);

        await Assert.That(result).IsEqualTo("Field 1 (varint): 42\nField 2 (string): \"hi\"");
    }
}
