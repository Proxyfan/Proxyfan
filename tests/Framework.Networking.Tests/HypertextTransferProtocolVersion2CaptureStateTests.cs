using System.Collections.Generic;
using System.Threading.Tasks;

namespace Proxyfan.Framework.Networking.Tests;

/// <summary>
///     Tests for <see cref="HypertextTransferProtocolVersion2CaptureState" />.
/// </summary>
public sealed class HypertextTransferProtocolVersion2CaptureStateTests
{
    /// <summary>
    ///     Verifies the constructor records the supplied stream identifier and starts with
    ///     empty request and response buffers and no END_STREAM flags set.
    /// </summary>
    [Test]
    public async Task Constructor_AnyStreamIdentifier_StartsEmptyAndUnended()
    {
        var capture = new HypertextTransferProtocolVersion2CaptureState(7);

        await Assert.That(capture.StreamIdentifier).IsEqualTo(7u);
        await Assert.That(capture.RequestBody.Length).IsEqualTo(0);
        await Assert.That(capture.ResponseBody.Length).IsEqualTo(0);
        await Assert.That(capture.RequestHeaders.Count).IsEqualTo(0);
        await Assert.That(capture.ResponseHeaders.Count).IsEqualTo(0);
        await Assert.That(capture.IsRequestEnded).IsFalse();
        await Assert.That(capture.IsResponseEnded).IsFalse();
    }

    /// <summary>
    ///     Verifies AppendRequestData stores bytes and that END_STREAM updates the flag once.
    /// </summary>
    [Test]
    public async Task AppendRequestData_TwoChunksLastWithEndStream_BuffersBothAndMarksEnded()
    {
        var capture = new HypertextTransferProtocolVersion2CaptureState(1);
        capture.AppendRequestData(new byte[] { 1, 2 }, isEndStream: false);
        capture.AppendRequestData(new byte[] { 3 }, isEndStream: true);

        await Assert.That(capture.RequestBody.ToArray()).IsEquivalentTo(new byte[] { 1, 2, 3 });
        await Assert.That(capture.IsRequestEnded).IsTrue();
    }

    /// <summary>
    ///     Verifies AppendResponseData mirrors the request-side behaviour.
    /// </summary>
    [Test]
    public async Task AppendResponseData_TwoChunksLastWithEndStream_BuffersBothAndMarksEnded()
    {
        var capture = new HypertextTransferProtocolVersion2CaptureState(1);
        capture.AppendResponseData(new byte[] { 4, 5 }, isEndStream: false);
        capture.AppendResponseData(new byte[] { 6 }, isEndStream: true);

        await Assert.That(capture.ResponseBody.ToArray()).IsEquivalentTo(new byte[] { 4, 5, 6 });
        await Assert.That(capture.IsResponseEnded).IsTrue();
    }

    /// <summary>
    ///     Verifies AppendRequestHeaders concatenates header lists.
    /// </summary>
    [Test]
    public async Task AppendRequestHeaders_TwoBatches_ConcatenatesAndOptionallyEnds()
    {
        var capture = new HypertextTransferProtocolVersion2CaptureState(1);
        var first = new List<HypertextTransferProtocolVersion2HpackHeaderField>
        {
            new(":method", "GET"),
        };
        var second = new List<HypertextTransferProtocolVersion2HpackHeaderField>
        {
            new(":path", "/"),
            new(":authority", "example.com"),
        };
        capture.AppendRequestHeaders(first, isEndStream: false);
        capture.AppendRequestHeaders(second, isEndStream: true);

        await Assert.That(capture.RequestHeaders.Count).IsEqualTo(3);
        await Assert.That(capture.IsRequestEnded).IsTrue();
    }

    /// <summary>
    ///     Verifies AppendResponseHeaders concatenates header lists.
    /// </summary>
    [Test]
    public async Task AppendResponseHeaders_TwoBatches_ConcatenatesAndOptionallyEnds()
    {
        var capture = new HypertextTransferProtocolVersion2CaptureState(1);
        var first = new List<HypertextTransferProtocolVersion2HpackHeaderField>
        {
            new(":status", "200"),
        };
        var second = new List<HypertextTransferProtocolVersion2HpackHeaderField>
        {
            new("content-type", "text/plain"),
        };
        capture.AppendResponseHeaders(first, isEndStream: false);
        capture.AppendResponseHeaders(second, isEndStream: true);

        await Assert.That(capture.ResponseHeaders.Count).IsEqualTo(2);
        await Assert.That(capture.IsResponseEnded).IsTrue();
    }
}
