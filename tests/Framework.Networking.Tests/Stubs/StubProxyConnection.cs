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
public sealed class StubProxyConnection : IProxyConnection
{
    private readonly Pipe _pipe;

    /// <summary>
    ///     Gets the <see cref="PipeWriter" /> that feeds data into this connection's transport input.
    /// </summary>
    public PipeWriter Writer => _pipe.Writer;

    /// <inheritdoc />
    public EndPoint RemoteEndPoint { get; }

    /// <inheritdoc />
    public IDuplexPipe Transport { get; }

    /// <summary>
    ///     Initializes a new instance of <see cref="StubProxyConnection" />.
    /// </summary>
    public StubProxyConnection()
    {
        _pipe = new Pipe();
        RemoteEndPoint = new IPEndPoint(IPAddress.Loopback, 12345);
        Transport = new StubDuplexPipe(_pipe.Reader);
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync()
    {
        return ValueTask.CompletedTask;
    }

    private sealed class StubDuplexPipe : IDuplexPipe
    {
        public PipeReader Input { get; }

        public PipeWriter Output { get; }

        public StubDuplexPipe(PipeReader input)
        {
            Input = input;
            Output = PipeWriter.Create(Stream.Null);
        }
    }
}