using System.IO;
using System.IO.Pipelines;
using System.Net;
using System.Threading.Tasks;
using Proxyfan.Domain.Proxy;

namespace Proxyfan.Framework.Networking.Tests.Stubs;

/// <summary>
///     A stub implementation of <see cref="IProxyConnection" /> backed by an in-process
///     <see cref="Pipe" /> for testing the connection dispatcher.
/// </summary>
internal sealed class StubProxyConnection : IProxyConnection
{
    private readonly Pipe _pipe;

    /// <summary>Initializes a new instance of <see cref="StubProxyConnection" />.</summary>
    internal StubProxyConnection()
    {
        _pipe = new Pipe();
        Transport = new StubDuplexPipe(_pipe.Reader);
    }

    /// <summary>Gets the <see cref="PipeWriter" /> that feeds data into this connection's transport.</summary>
    internal PipeWriter Writer => _pipe.Writer;

    /// <inheritdoc />
    public IDuplexPipe Transport { get; }

    /// <inheritdoc />
    public EndPoint RemoteEndPoint { get; } = new IPEndPoint(IPAddress.Loopback, 12345);

    /// <inheritdoc />
    public ValueTask DisposeAsync()
    {
        return ValueTask.CompletedTask;
    }

    private sealed class StubDuplexPipe(PipeReader input) : IDuplexPipe
    {
        public PipeReader Input { get; } = input;

        public PipeWriter Output { get; } = PipeWriter.Create(Stream.Null);
    }
}
