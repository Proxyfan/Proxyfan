using System.IO.Pipelines;
using System.Net.Security;

namespace Proxyfan.Framework.Networking;

/// <summary>
///     Factory for <see cref="TransportLayerSecurityInterceptionPipes" />. Extracted from
///     <see cref="TransportLayerSecurityInterceptorHandler" /> to keep the handler under the
///     analyzer-enforced class size budget (ATXCS034).
/// </summary>
public static class TransportLayerSecurityInterceptionPipesFactory
{
    /// <summary>
    ///     Creates a four-pipe bridge over the supplied client and upstream TLS streams.
    /// </summary>
    /// <param name="clientSecureStream">The decrypted client-side TLS stream.</param>
    /// <param name="serverSecureStream">The decrypted upstream TLS stream.</param>
    /// <returns>A new <see cref="TransportLayerSecurityInterceptionPipes" /> bridging both streams.</returns>
    public static TransportLayerSecurityInterceptionPipes Create(SslStream clientSecureStream, SslStream serverSecureStream)
    {
        var readerOptions = new StreamPipeReaderOptions(leaveOpen: true);
        var writerOptions = new StreamPipeWriterOptions(leaveOpen: true);
        var clientReader = PipeReader.Create(clientSecureStream, readerOptions);
        var clientWriter = PipeWriter.Create(clientSecureStream, writerOptions);
        var serverReader = PipeReader.Create(serverSecureStream, readerOptions);
        var serverWriter = PipeWriter.Create(serverSecureStream, writerOptions);
        var pipes = new TransportLayerSecurityInterceptionPipes(clientReader, clientWriter, serverReader, serverWriter);
        return pipes;
    }
}
