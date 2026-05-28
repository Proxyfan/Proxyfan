using System;
using System.Buffers;
using System.IO.Pipelines;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Proxyfan.Framework.Networking.Tests;

/// <summary>
///     Unit tests for <see cref="PipeReaderDrainer" />. Verifies that bytes already buffered in a
///     <see cref="PipeReader" /> can be drained even after the previous reader marked all bytes as
///     examined (the exact state produced by the HTTP header parser).
/// </summary>
public sealed class PipeReaderDrainerTests
{
    /// <summary>Drains all buffered bytes when the reader has data available.</summary>
    [Test]
    public async Task DrainBufferedBytesAsync_BufferedData_ReturnsAllBytes()
    {
        var pipe = new Pipe();
        var payload = Encoding.ASCII.GetBytes("HELLO-WORLD");
        await pipe.Writer.WriteAsync(payload);
        await pipe.Writer.FlushAsync();
        var prepResult = await pipe.Reader.ReadAsync();
        pipe.Reader.AdvanceTo(prepResult.Buffer.Start, prepResult.Buffer.End);

        var drained = await PipeReaderDrainer.DrainBufferedBytesAsync(pipe.Reader, CancellationToken.None);

        await Assert.That(drained).IsEquivalentTo(payload);
    }

    /// <summary>Returns an empty array when no data is currently buffered.</summary>
    [Test]
    public async Task DrainBufferedBytesAsync_NoBufferedData_ReturnsEmpty()
    {
        var pipe = new Pipe();

        var drained = await PipeReaderDrainer.DrainBufferedBytesAsync(pipe.Reader, CancellationToken.None);

        await Assert.That(drained.Length).IsEqualTo(0);
    }

    /// <summary>After draining, subsequent reads see only newly written bytes.</summary>
    [Test]
    public async Task DrainBufferedBytesAsync_AfterDrain_ConsumedFromReader()
    {
        var pipe = new Pipe();
        var first = Encoding.ASCII.GetBytes("FIRST");
        await pipe.Writer.WriteAsync(first);
        await pipe.Writer.FlushAsync();
        var prepResult = await pipe.Reader.ReadAsync();
        pipe.Reader.AdvanceTo(prepResult.Buffer.Start, prepResult.Buffer.End);

        var drained = await PipeReaderDrainer.DrainBufferedBytesAsync(pipe.Reader, CancellationToken.None);
        await Assert.That(drained).IsEquivalentTo(first);

        var second = Encoding.ASCII.GetBytes("SECOND");
        await pipe.Writer.WriteAsync(second);
        await pipe.Writer.FlushAsync();

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var nextResult = await pipe.Reader.ReadAsync(cts.Token);
        var nextBytes = nextResult.Buffer.ToArray();
        pipe.Reader.AdvanceTo(nextResult.Buffer.End);

        await Assert.That(nextBytes).IsEquivalentTo(second);
    }
}
