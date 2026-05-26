using Proxyfan.Domain.Proxy;
using System.IO.Pipelines;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace Proxyfan.Framework.Networking;

/// <summary>
///     An <see cref="IProxyConnection" /> implementation that wraps an accepted <see cref="Socket" />
///     and exposes it as a duplex pipe via <see cref="System.IO.Pipelines" />.
/// </summary>
public sealed class SocketConnection : IProxyConnection
{
    private readonly NetworkStream _stream;
    private bool _isDisposed;

    /// <summary>
    ///     Initializes a new instance of <see cref="SocketConnection" /> for the given accepted socket.
    /// </summary>
    /// <param name="socket">The accepted TCP socket. Ownership is transferred to this instance.</param>
    public SocketConnection(Socket socket)
    {
        var stream = new NetworkStream(socket, ownsSocket: true);
        _stream = stream;
        var fallbackEndPoint = new IPEndPoint(IPAddress.None, 0);
        RemoteEndPoint = socket.RemoteEndPoint ?? fallbackEndPoint;
        var pipe = new DuplexPipe(PipeReader.Create(stream), PipeWriter.Create(stream));
        Transport = pipe;
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_isDisposed)
        {
            return;
        }

        _isDisposed = true;
        await _stream.DisposeAsync().ConfigureAwait(false);
    }

    /// <inheritdoc />
    public EndPoint RemoteEndPoint { get; }

    /// <inheritdoc />
    public IDuplexPipe Transport { get; }

    private sealed class DuplexPipe : IDuplexPipe
    {
        public DuplexPipe(PipeReader input, PipeWriter output)
        {
            Input = input;
            Output = output;
        }

        public PipeReader Input { get; }

        public PipeWriter Output { get; }
    }
}