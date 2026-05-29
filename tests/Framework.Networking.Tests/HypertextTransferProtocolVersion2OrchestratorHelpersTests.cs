using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Proxyfan.Framework.Networking.Tests;

/// <summary>
///     Tests for <see cref="HypertextTransferProtocolVersion2OrchestratorHelpers" />.
/// </summary>
public sealed class HypertextTransferProtocolVersion2OrchestratorHelpersTests
{
    /// <summary>
    ///     Verifies that <see cref="HypertextTransferProtocolVersion2OrchestratorHelpers.BuildDescriptor" />
    ///     copies every field from the supplied frame header.
    /// </summary>
    [Test]
    public async Task BuildDescriptor_AnyFrame_CopiesAllFields()
    {
        var header = new HypertextTransferProtocolVersion2FrameHeader(
            length: 4,
            rawType: (byte)HypertextTransferProtocolVersion2FrameType.Data,
            flags: HypertextTransferProtocolVersion2FrameFlag.EndStreamOrAcknowledge,
            streamIdentifier: 5);
        var frame = new HypertextTransferProtocolVersion2Frame(header, new byte[] { 1, 2, 3, 4 });

        var descriptor = HypertextTransferProtocolVersion2OrchestratorHelpers.BuildDescriptor(frame);

        await Assert.That(descriptor.Type).IsEqualTo(HypertextTransferProtocolVersion2FrameType.Data);
        await Assert.That(descriptor.Flags).IsEqualTo(HypertextTransferProtocolVersion2FrameFlag.EndStreamOrAcknowledge);
        await Assert.That(descriptor.StreamIdentifier).IsEqualTo(5u);
        await Assert.That(descriptor.PayloadLength).IsEqualTo(4);
    }

    /// <summary>
    ///     Verifies that <see cref="HypertextTransferProtocolVersion2OrchestratorHelpers.BuildResponseFromHeaders" />
    ///     extracts the <c>:status</c> pseudo-header into <see cref="Proxyfan.Domain.Traffic.HypertextTransferProtocolResponseData.StatusCode" />.
    /// </summary>
    [Test]
    public async Task BuildResponseFromHeaders_ValidStatus_ReturnsResponseWithStatusCode()
    {
        var headers = new List<HypertextTransferProtocolVersion2HpackHeaderField>
        {
            new(":status", "204"),
            new("content-type", "text/plain"),
        };

        var response = HypertextTransferProtocolVersion2OrchestratorHelpers.BuildResponseFromHeaders(headers, new byte[] { 1, 2 });

        await Assert.That(response).IsNotNull();
        await Assert.That(response!.StatusCode).IsEqualTo(204);
        await Assert.That(response.Headers.Get("content-type")).IsEqualTo("text/plain");
        await Assert.That(response.Body.ToArray()).IsEquivalentTo(new byte[] { 1, 2 });
        await Assert.That(response.Version).IsEqualTo("HTTP/2");
    }

    /// <summary>
    ///     Verifies that a missing <c>:status</c> pseudo-header yields a null response.
    /// </summary>
    [Test]
    public async Task BuildResponseFromHeaders_MissingStatus_ReturnsNull()
    {
        var headers = new List<HypertextTransferProtocolVersion2HpackHeaderField>
        {
            new("content-type", "text/plain"),
        };

        var response = HypertextTransferProtocolVersion2OrchestratorHelpers.BuildResponseFromHeaders(headers, ReadOnlyMemory<byte>.Empty);

        await Assert.That(response).IsNull();
    }

    /// <summary>
    ///     Verifies that a non-numeric <c>:status</c> value yields a null response.
    /// </summary>
    [Test]
    public async Task BuildResponseFromHeaders_NonNumericStatus_ReturnsNull()
    {
        var headers = new List<HypertextTransferProtocolVersion2HpackHeaderField>
        {
            new(":status", "abc"),
        };

        var response = HypertextTransferProtocolVersion2OrchestratorHelpers.BuildResponseFromHeaders(headers, ReadOnlyMemory<byte>.Empty);

        await Assert.That(response).IsNull();
    }

    /// <summary>
    ///     Verifies that an out-of-range <c>:status</c> value yields a null response.
    /// </summary>
    [Test]
    public async Task BuildResponseFromHeaders_StatusOutOfRange_ReturnsNull()
    {
        var headers = new List<HypertextTransferProtocolVersion2HpackHeaderField>
        {
            new(":status", "1001"),
        };

        var response = HypertextTransferProtocolVersion2OrchestratorHelpers.BuildResponseFromHeaders(headers, ReadOnlyMemory<byte>.Empty);

        await Assert.That(response).IsNull();
    }

    /// <summary>
    ///     Verifies that unknown pseudo-headers (other than <c>:status</c>) are dropped
    ///     instead of being copied into the regular header collection.
    /// </summary>
    [Test]
    public async Task BuildResponseFromHeaders_OtherPseudoHeaders_AreSkipped()
    {
        var headers = new List<HypertextTransferProtocolVersion2HpackHeaderField>
        {
            new(":status", "200"),
            new(":scheme", "https"),
            new("x-real", "value"),
        };

        var response = HypertextTransferProtocolVersion2OrchestratorHelpers.BuildResponseFromHeaders(headers, ReadOnlyMemory<byte>.Empty);

        await Assert.That(response).IsNotNull();
        await Assert.That(response!.Headers.HasHeader("x-real")).IsTrue();
        await Assert.That(response.Headers.HasHeader(":scheme")).IsFalse();
    }

    /// <summary>
    ///     Verifies that <see cref="HypertextTransferProtocolVersion2OrchestratorHelpers.CreateCaptureState" />
    ///     returns a fresh state tracker for the supplied stream id.
    /// </summary>
    [Test]
    public async Task CreateCaptureState_AnyStreamIdentifier_ReturnsFreshState()
    {
        var capture = HypertextTransferProtocolVersion2OrchestratorHelpers.CreateCaptureState(42);

        await Assert.That(capture.StreamIdentifier).IsEqualTo(42u);
        await Assert.That(capture.IsRequestEnded).IsFalse();
        await Assert.That(capture.IsResponseEnded).IsFalse();
    }

    /// <summary>
    ///     Verifies that <see cref="HypertextTransferProtocolVersion2OrchestratorHelpers.ReplaceResponseBody" />
    ///     swaps the body while keeping every other field of the response intact.
    /// </summary>
    [Test]
    public async Task ReplaceResponseBody_NewBody_OverridesBodyButKeepsHeaders()
    {
        var headers = Proxyfan.Domain.Traffic.HeaderCollection.Empty.Add("content-type", "text/plain");
        var parameters = new Proxyfan.Domain.Traffic.HypertextTransferProtocolResponseDataParameters
        {
            Body = new byte[] { 1, 2 },
            Headers = headers,
            ReasonPhrase = "OK",
            StatusCode = 200,
            Version = "HTTP/2",
        };
        var original = new Proxyfan.Domain.Traffic.HypertextTransferProtocolResponseData(parameters);

        var replaced = HypertextTransferProtocolVersion2OrchestratorHelpers.ReplaceResponseBody(original, new byte[] { 9, 8, 7 });

        await Assert.That(replaced.StatusCode).IsEqualTo(200);
        await Assert.That(replaced.Headers.Get("content-type")).IsEqualTo("text/plain");
        await Assert.That(replaced.Body.ToArray()).IsEquivalentTo(new byte[] { 9, 8, 7 });
        await Assert.That(replaced.Version).IsEqualTo("HTTP/2");
    }
}
