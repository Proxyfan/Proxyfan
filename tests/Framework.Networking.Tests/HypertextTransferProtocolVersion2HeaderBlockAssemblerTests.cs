using System.Threading.Tasks;

namespace Proxyfan.Framework.Networking.Tests;

/// <summary>
///     Tests for <see cref="HypertextTransferProtocolVersion2HeaderBlockAssembler" />.
/// </summary>
public sealed class HypertextTransferProtocolVersion2HeaderBlockAssemblerTests
{
    /// <summary>
    ///     A single-frame block (END_HEADERS set) is returned immediately as the complete fragment.
    /// </summary>
    [Test]
    public async Task BeginBlock_EndHeadersSet_ReturnsFragmentImmediately()
    {
        var assembler = new HypertextTransferProtocolVersion2HeaderBlockAssembler();
        byte[] fragment = [0x82];

        var result = assembler.BeginBlock(1, fragment, hasEndHeadersFlag: true);

        await Assert.That(result).IsNotNull();
        await Assert.That(result!).IsEquivalentTo(fragment);
    }

    /// <summary>
    ///     A multi-CONTINUATION block returns the concatenated fragment only on the final
    ///     END_HEADERS frame.
    /// </summary>
    [Test]
    public async Task AppendContinuation_AcrossMultipleFrames_ReturnsConcatenated()
    {
        var assembler = new HypertextTransferProtocolVersion2HeaderBlockAssembler();
        var pending = assembler.BeginBlock(1, [0x01], hasEndHeadersFlag: false);
        await Assert.That(pending).IsNull();

        pending = assembler.AppendContinuation(1, [0x02, 0x03], hasEndHeadersFlag: false);
        await Assert.That(pending).IsNull();

        pending = assembler.AppendContinuation(1, [0x04], hasEndHeadersFlag: true);

        await Assert.That(pending).IsNotNull();
        await Assert.That(pending!).IsEquivalentTo(new byte[] { 0x01, 0x02, 0x03, 0x04 });
    }

    /// <summary>
    ///     A CONTINUATION whose stream id does not match the pending block resets the assembler
    ///     and returns null.
    /// </summary>
    [Test]
    public async Task AppendContinuation_MismatchedStreamIdentifier_ReturnsNull()
    {
        var assembler = new HypertextTransferProtocolVersion2HeaderBlockAssembler();
        assembler.BeginBlock(1, [0x01], hasEndHeadersFlag: false);

        var result = assembler.AppendContinuation(3, [0x02], hasEndHeadersFlag: true);

        await Assert.That(result).IsNull();
        await Assert.That(assembler.IsInProgress).IsFalse();
    }

    /// <summary>
    ///     A continuation arriving with no pending block is invalid and returns null.
    /// </summary>
    [Test]
    public async Task AppendContinuation_NoPendingBlock_ReturnsNull()
    {
        var assembler = new HypertextTransferProtocolVersion2HeaderBlockAssembler();

        var result = assembler.AppendContinuation(1, [0x01], hasEndHeadersFlag: true);

        await Assert.That(result).IsNull();
    }

    /// <summary>
    ///     A block exceeding the configured maximum size is rejected and the assembler resets.
    /// </summary>
    [Test]
    public async Task BeginBlock_ExceedsMaximumSize_ReturnsNull()
    {
        var assembler = new HypertextTransferProtocolVersion2HeaderBlockAssembler(maximumByteSize: 4);
        byte[] fragment = [0, 1, 2, 3, 4];

        var result = assembler.BeginBlock(1, fragment, hasEndHeadersFlag: true);

        await Assert.That(result).IsNull();
        await Assert.That(assembler.IsInProgress).IsFalse();
    }

    /// <summary>
    ///     A continuation that overflows the configured cap is rejected and the assembler resets.
    /// </summary>
    [Test]
    public async Task AppendContinuation_OverflowsMaximumSize_ReturnsNull()
    {
        var assembler = new HypertextTransferProtocolVersion2HeaderBlockAssembler(maximumByteSize: 4);
        assembler.BeginBlock(1, [0x01, 0x02], hasEndHeadersFlag: false);

        var result = assembler.AppendContinuation(1, [0x03, 0x04, 0x05], hasEndHeadersFlag: true);

        await Assert.That(result).IsNull();
        await Assert.That(assembler.IsInProgress).IsFalse();
    }

    /// <summary>
    ///     <see cref="HypertextTransferProtocolVersion2HeaderBlockAssembler.BeginBlock" /> while
    ///     another block is in progress is treated as a protocol violation: the new BeginBlock
    ///     returns null and the assembler retains its in-progress state until callers Reset.
    /// </summary>
    [Test]
    public async Task BeginBlock_AnotherInProgress_ReturnsNull()
    {
        var assembler = new HypertextTransferProtocolVersion2HeaderBlockAssembler();
        assembler.BeginBlock(1, [0x01], hasEndHeadersFlag: false);

        var result = assembler.BeginBlock(3, [0x02], hasEndHeadersFlag: true);

        await Assert.That(result).IsNull();
        await Assert.That(assembler.IsInProgress).IsTrue();
    }

    /// <summary>
    ///     <see cref="HypertextTransferProtocolVersion2HeaderBlockAssembler.CurrentByteSize" />
    ///     reports the buffered fragment count between BeginBlock and the final CONTINUATION.
    /// </summary>
    [Test]
    public async Task CurrentByteSize_BetweenContinuations_TracksBufferedBytes()
    {
        var assembler = new HypertextTransferProtocolVersion2HeaderBlockAssembler();

        assembler.BeginBlock(1, [0x01, 0x02, 0x03], hasEndHeadersFlag: false);

        await Assert.That(assembler.CurrentByteSize).IsEqualTo(3);

        assembler.AppendContinuation(1, [0x04, 0x05], hasEndHeadersFlag: false);

        await Assert.That(assembler.CurrentByteSize).IsEqualTo(5);
    }
}
