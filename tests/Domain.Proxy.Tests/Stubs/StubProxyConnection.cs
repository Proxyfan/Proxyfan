using System.IO;
using System.IO.Pipelines;
using System.Net;
using System.Threading.Tasks;

namespace Proxyfan.Domain.Proxy.Tests.Stubs;

/// <summary>
///     A minimal no-op stub implementation of <see cref="IProxyConnection" /> for
///     exercising connection-dispatch paths inside <see cref="ProxyServer" /> tests.
/// </summary>
public sealed class StubProxyConnection : IProxyConnection
{
    /// <inheritdoc />
    public EndPoint RemoteEndPoint { get; } = new IPEndPoint(IPAddress.Loopback, 54321);

    /// <inheritdoc />
    public IDuplexPipe Transport { get; } = new NullDuplexPipe();

    /// <inheritdoc />
    public ValueTask DisposeAsync()
    {
        return ValueTask.CompletedTask;
    }

    private sealed class NullDuplexPipe : IDuplexPipe
    {
        public PipeReader Input { get; } = PipeReader.Create(Stream.Null);

        public PipeWriter Output { get; } = PipeWriter.Create(Stream.Null);
    }
}
