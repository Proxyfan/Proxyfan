using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;

namespace Proxyfan.Framework.Networking;

/// <summary>
///     Holds the four pipelines used by the TLS interceptor to bridge between the client and
///     upstream <see cref="System.Net.Security.SslStream" /> instances:
///     reader/writer for each side.
/// </summary>
public sealed class TransportLayerSecurityInterceptionPipes
{
    /// <summary>
    ///     Gets the pipe reader for decrypted bytes from the client TLS stream.
    /// </summary>
    public PipeReader ClientReader { get; }

    /// <summary>
    ///     Gets the pipe writer that encrypts bytes destined for the client.
    /// </summary>
    public PipeWriter ClientWriter { get; }

    /// <summary>
    ///     Gets the pipe reader for decrypted bytes from the upstream TLS stream.
    /// </summary>
    public PipeReader ServerReader { get; }

    /// <summary>
    ///     Gets the pipe writer that encrypts bytes destined for the upstream server.
    /// </summary>
    public PipeWriter ServerWriter { get; }

    /// <summary>
    ///     Initializes a new <see cref="TransportLayerSecurityInterceptionPipes" /> with the supplied pipes.
    /// </summary>
    /// <param name="clientReader">The pipe reader that consumes decrypted bytes from the client TLS stream.</param>
    /// <param name="clientWriter">The pipe writer that encrypts bytes destined for the client.</param>
    /// <param name="serverReader">The pipe reader that consumes decrypted bytes from the upstream TLS stream.</param>
    /// <param name="serverWriter">The pipe writer that encrypts bytes destined for the upstream server.</param>
    public TransportLayerSecurityInterceptionPipes(
        PipeReader clientReader,
        PipeWriter clientWriter,
        PipeReader serverReader,
        PipeWriter serverWriter)
    {
        ClientReader = clientReader;
        ClientWriter = clientWriter;
        ServerReader = serverReader;
        ServerWriter = serverWriter;
    }

    /// <summary>
    ///     Completes all four pipes signalling end-of-stream to their consumers.
    /// </summary>
    /// <param name="cancellationToken">A token that cancels the completion (unused; provided to satisfy async conventions).</param>
    /// <returns>A task that completes when all four pipes have been completed.</returns>
    public Task CompleteAsync(CancellationToken cancellationToken)
    {
        _ = cancellationToken;
        var completionTask = Task.WhenAll(
            ClientReader.CompleteAsync().AsTask(),
            ClientWriter.CompleteAsync().AsTask(),
            ServerReader.CompleteAsync().AsTask(),
            ServerWriter.CompleteAsync().AsTask());
        return completionTask;
    }
}
