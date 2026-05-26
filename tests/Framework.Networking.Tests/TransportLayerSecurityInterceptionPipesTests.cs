using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;

namespace Proxyfan.Framework.Networking.Tests;

/// <summary>
///     Tests for <see cref="TransportLayerSecurityInterceptionPipes" />.
/// </summary>
public sealed class TransportLayerSecurityInterceptionPipesTests
{
    /// <summary>
    ///     Verifies that the constructor stores each pipe in the corresponding property.
    /// </summary>
    [Test]
    public async Task Constructor_WithFourPipes_StoresInProperties()
    {
        var clientPipe = new Pipe();
        var serverPipe = new Pipe();
        var altClientPipe = new Pipe();
        var altServerPipe = new Pipe();

        var pipes = new TransportLayerSecurityInterceptionPipes(
            clientPipe.Reader,
            altClientPipe.Writer,
            serverPipe.Reader,
            altServerPipe.Writer);

        await Assert.That(pipes.ClientReader).IsSameReferenceAs(clientPipe.Reader);
        await Assert.That(pipes.ClientWriter).IsSameReferenceAs(altClientPipe.Writer);
        await Assert.That(pipes.ServerReader).IsSameReferenceAs(serverPipe.Reader);
        await Assert.That(pipes.ServerWriter).IsSameReferenceAs(altServerPipe.Writer);
    }

    /// <summary>
    ///     Verifies that <see cref="TransportLayerSecurityInterceptionPipes.CompleteAsync" />
    ///     completes all four underlying pipes (signalled by the writer-side seeing IsCompleted).
    /// </summary>
    [Test]
    public async Task CompleteAsync_AfterConstruction_CompletesAllFourPipes()
    {
        var clientPipe = new Pipe();
        var serverPipe = new Pipe();
        var pipes = new TransportLayerSecurityInterceptionPipes(
            clientPipe.Reader,
            clientPipe.Writer,
            serverPipe.Reader,
            serverPipe.Writer);

        await pipes.CompleteAsync(CancellationToken.None);

        // After completion, attempts to write to a completed PipeWriter throw
        // InvalidOperationException, confirming both writers were completed.
        var clientWriteThrew = false;
        try
        {
            await clientPipe.Writer.WriteAsync(new byte[] { 1 }, CancellationToken.None);
        }
        catch (System.InvalidOperationException)
        {
            clientWriteThrew = true;
        }

        var serverWriteThrew = false;
        try
        {
            await serverPipe.Writer.WriteAsync(new byte[] { 1 }, CancellationToken.None);
        }
        catch (System.InvalidOperationException)
        {
            serverWriteThrew = true;
        }

        await Assert.That(clientWriteThrew).IsTrue();
        await Assert.That(serverWriteThrew).IsTrue();
    }
}
