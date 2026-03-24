using System;
using System.IO.Pipelines;
using System.Net;

namespace Proxyfan.Domain.Proxy;

/// <summary>
///     Represents an accepted TCP connection before protocol detection.
///     Provides duplex pipe transport and remote endpoint information.
/// </summary>
public interface IProxyConnection : IAsyncDisposable
{
    /// <summary>Gets the duplex pipe transport for reading and writing data on this connection.</summary>
    IDuplexPipe Transport { get; }

    /// <summary>Gets the remote endpoint of the connected client.</summary>
    EndPoint RemoteEndPoint { get; }
}
