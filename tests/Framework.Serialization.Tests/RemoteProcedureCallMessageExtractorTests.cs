using System;
using System.IO;
using System.Threading.Tasks;

namespace Proxyfan.Framework.Serialization.Tests;

/// <summary>
///     Tests for <see cref="RemoteProcedureCallMessageExtractor" /> and
///     <see cref="RemoteProcedureCallExtractionResult" />.
/// </summary>
public sealed class RemoteProcedureCallMessageExtractorTests
{
    /// <summary>
    ///     Verifies that an empty buffer extracts no messages and consumes zero bytes.
    /// </summary>
    [Test]
    public async Task ExtractAvailable_EmptyBuffer_ReturnsNoMessages()
    {
        var result = RemoteProcedureCallMessageExtractor.ExtractAvailable(System.Array.Empty<byte>());

        await Assert.That(result.Messages.Count).IsEqualTo(0);
        await Assert.That(result.BytesConsumed).IsEqualTo(0);
    }

    /// <summary>
    ///     Verifies that a buffer shorter than the 5-byte prefix consumes nothing.
    /// </summary>
    [Test]
    public async Task ExtractAvailable_BufferShorterThanPrefix_ConsumesNothing()
    {
        var bytes = new byte[] { 0x00, 0x00, 0x00 };

        var result = RemoteProcedureCallMessageExtractor.ExtractAvailable(bytes);

        await Assert.That(result.Messages.Count).IsEqualTo(0);
        await Assert.That(result.BytesConsumed).IsEqualTo(0);
    }

    /// <summary>
    ///     Verifies that a single uncompressed message is extracted.
    /// </summary>
    [Test]
    public async Task ExtractAvailable_SingleUncompressedMessage_ExtractsPayload()
    {
        var payload = new byte[] { 0xDE, 0xAD, 0xBE, 0xEF };
        var bytes = new byte[5 + payload.Length];
        bytes[0] = 0x00;
        bytes[4] = (byte)payload.Length;
        payload.CopyTo(bytes, 5);

        var result = RemoteProcedureCallMessageExtractor.ExtractAvailable(bytes);

        await Assert.That(result.Messages.Count).IsEqualTo(1);
        await Assert.That(result.Messages[0].IsCompressed).IsFalse();
        await Assert.That(result.Messages[0].Payload.Length).IsEqualTo(4);
        await Assert.That(result.BytesConsumed).IsEqualTo(bytes.Length);
    }

    /// <summary>
    ///     Verifies that compression flag = 1 sets IsCompressed.
    /// </summary>
    [Test]
    public async Task ExtractAvailable_CompressedFlag_SetsIsCompressed()
    {
        var bytes = new byte[] { 0x01, 0, 0, 0, 1, 0xAB };

        var result = RemoteProcedureCallMessageExtractor.ExtractAvailable(bytes);

        await Assert.That(result.Messages.Count).IsEqualTo(1);
        await Assert.That(result.Messages[0].IsCompressed).IsTrue();
    }

    /// <summary>
    ///     Verifies that multiple consecutive messages are all extracted.
    /// </summary>
    [Test]
    public async Task ExtractAvailable_TwoMessages_ExtractsBoth()
    {
        var bytes = new byte[] { 0x00, 0, 0, 0, 1, 0xAA, 0x00, 0, 0, 0, 2, 0xBB, 0xCC };

        var result = RemoteProcedureCallMessageExtractor.ExtractAvailable(bytes);

        await Assert.That(result.Messages.Count).IsEqualTo(2);
        await Assert.That(result.Messages[0].Payload.Length).IsEqualTo(1);
        await Assert.That(result.Messages[1].Payload.Length).IsEqualTo(2);
        await Assert.That(result.BytesConsumed).IsEqualTo(bytes.Length);
    }

    /// <summary>
    ///     Verifies that a partial trailing frame stops extraction at the message boundary.
    /// </summary>
    [Test]
    public async Task ExtractAvailable_PartialTrailingFrame_StopsAtBoundary()
    {
        var bytes = new byte[] { 0x00, 0, 0, 0, 1, 0xAA, 0x00, 0, 0, 0, 5, 0xBB };

        var result = RemoteProcedureCallMessageExtractor.ExtractAvailable(bytes);

        await Assert.That(result.Messages.Count).IsEqualTo(1);
        await Assert.That(result.BytesConsumed).IsEqualTo(6);
    }

    /// <summary>
    ///     Verifies that a payload that would exceed int.MaxValue throws.
    /// </summary>
    [Test]
    public async Task ExtractAvailable_PayloadExceedingMaxInt_Throws()
    {
        var bytes = new byte[] { 0x00, 0xFF, 0xFF, 0xFF, 0xFF };

        await Assert.That(() => RemoteProcedureCallMessageExtractor.ExtractAvailable(bytes)).Throws<InvalidDataException>();
    }

    /// <summary>
    ///     Verifies that a zero-length message is valid (e.g. trailers-only response).
    /// </summary>
    [Test]
    public async Task ExtractAvailable_ZeroLengthMessage_Extracts()
    {
        var bytes = new byte[] { 0x00, 0, 0, 0, 0 };

        var result = RemoteProcedureCallMessageExtractor.ExtractAvailable(bytes);

        await Assert.That(result.Messages.Count).IsEqualTo(1);
        await Assert.That(result.Messages[0].Payload.Length).IsEqualTo(0);
    }
}
