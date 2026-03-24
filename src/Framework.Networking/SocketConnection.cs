using System.IO.Pipelines;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using Proxyfan.Domain.Proxy;

namespace Proxyfan.Framework.Networking;

/// <summary>
///     An <see cref="IProxyConnection" /> implementation that wraps an accepted <see cref="Socket" />
///     and exposes it as a duplex pipe via <see cref="System.IO.Pipelines" />.
/// </summary>
internal sealed class SocketConnection : IProxyConnection
{
    private readonly NetworkStream _stream;
    private bool _disposed;

    /// <summary>
    ///     Initializes a new instance of <see cref="SocketConnection" /> for the given accepted socket.
    /// </summary>
    /// <param name="socket">The accepted TCP socket. Ownership is transferred to this instance.</param>
    internal SocketConnection(Socket socket)
    {
        _stream = new NetworkStream(socket, ownsSocket: true);
        RemoteEndPoint = socket.RemoteEndPoint ?? new IPEndPoint(IPAddress.None, 0);
        Transport = new DuplexPipe(PipeReader.Create(_stream), PipeWriter.Create(_stream));
    }

    /// <inheritdoc />
    public IDuplexPipe Transport { get; }

    /// <inheritdoc />
    public EndPoint RemoteEndPoint { get; }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        await _stream.DisposeAsync().ConfigureAwait(false);
    }

    private sealed class DuplexPipe : IDuplexPipe
    {
        internal DuplexPipe(PipeReader input, PipeWriter output)
        {
            Input = input;
            Output = output;
        }

        public PipeReader Input { get; }

        public PipeWriter Output { get; }
    }
}
