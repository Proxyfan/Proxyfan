using Proxyfan.Framework.Serialization;
using System;
using System.Text;
using System.Threading.Tasks;
using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.Core;

namespace Proxyfan.Framework.Serialization.Tests;

/// <summary>
///     Tests for <see cref="HexDumpFormatter" />.
/// </summary>
public sealed class HexDumpFormatterTests
{
    [Test]
    public async Task Format_Empty_ReturnsEmpty()
    {
        var result = HexDumpFormatter.Format(ReadOnlySpan<byte>.Empty);

        await Assert.That(result).IsEqualTo(string.Empty);
    }

    [Test]
    public async Task Format_SingleByte_RendersOffsetHexAndAscii()
    {
        var result = HexDumpFormatter.Format(new byte[] { 0x41 });

        await Assert.That(result.StartsWith("00000000  41", StringComparison.Ordinal)).IsTrue();
        await Assert.That(result.EndsWith('A')).IsTrue();
    }

    [Test]
    public async Task Format_NonPrintableBytes_ShownAsDots()
    {
        var result = HexDumpFormatter.Format(new byte[] { 0x00, 0x01, 0x02 });

        await Assert.That(result.Contains("...")).IsTrue();
    }

    [Test]
    public async Task Format_SixteenBytes_FitsOnSingleLine()
    {
        var bytes = new byte[16];

        for (var index = 0; index < bytes.Length; index++)
        {
            bytes[index] = (byte)(0x40 + index);
        }

        var result = HexDumpFormatter.Format(bytes);

        await Assert.That(result.Contains('\n')).IsFalse();
        await Assert.That(result.EndsWith("@ABCDEFGHIJKLMNO", StringComparison.Ordinal)).IsTrue();
    }

    [Test]
    public async Task Format_SeventeenBytes_RendersTwoLines()
    {
        var bytes = new byte[17];

        for (var index = 0; index < bytes.Length; index++)
        {
            bytes[index] = (byte)(0x41 + index);
        }

        var result = HexDumpFormatter.Format(bytes);
        var lines = result.Split('\n');

        await Assert.That(lines.Length).IsEqualTo(2);
        await Assert.That(lines[1].StartsWith("00000010", StringComparison.Ordinal)).IsTrue();
    }

    [Test]
    public async Task Format_AsciiBytes_RendersPrintableCharactersInAsciiColumn()
    {
        var bytes = Encoding.ASCII.GetBytes("Hello!");

        var result = HexDumpFormatter.Format(bytes);

        await Assert.That(result.EndsWith("Hello!", StringComparison.Ordinal)).IsTrue();
    }
}
