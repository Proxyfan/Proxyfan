#### [Domain\.Proxy](index.md 'index')
### [Proxyfan\.Domain\.Proxy](Proxyfan.Domain.Proxy.md 'Proxyfan\.Domain\.Proxy')

## IProxyConnection Interface

Represents an accepted TCP connection before protocol detection\.
Provides duplex pipe transport and remote endpoint information\.

```csharp
public interface IProxyConnection : System.IAsyncDisposable
```

Implements [System\.IAsyncDisposable](https://learn.microsoft.com/en-us/dotnet/api/system.iasyncdisposable 'System\.IAsyncDisposable')
### Properties

<a name='Proxyfan.Domain.Proxy.IProxyConnection.RemoteEndPoint'></a>

## IProxyConnection\.RemoteEndPoint Property

Gets the remote endpoint of the connected client\.

```csharp
System.Net.EndPoint RemoteEndPoint { get; }
```

#### Property Value
[System\.Net\.EndPoint](https://learn.microsoft.com/en-us/dotnet/api/system.net.endpoint 'System\.Net\.EndPoint')

<a name='Proxyfan.Domain.Proxy.IProxyConnection.Transport'></a>

## IProxyConnection\.Transport Property

Gets the duplex pipe transport for reading and writing data on this connection\.

```csharp
System.IO.Pipelines.IDuplexPipe Transport { get; }
```

#### Property Value
[System\.IO\.Pipelines\.IDuplexPipe](https://learn.microsoft.com/en-us/dotnet/api/system.io.pipelines.iduplexpipe 'System\.IO\.Pipelines\.IDuplexPipe')