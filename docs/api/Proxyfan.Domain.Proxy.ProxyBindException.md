#### [Domain\.Proxy](index.md 'index')
### [Proxyfan\.Domain\.Proxy](Proxyfan.Domain.Proxy.md 'Proxyfan\.Domain\.Proxy')

## ProxyBindException Class

The exception thrown when the proxy listener fails to bind to the configured port,
for example because the port is already in use or access is denied\.

```csharp
public sealed class ProxyBindException : System.Exception
```

Inheritance [System\.Object](https://learn.microsoft.com/en-us/dotnet/api/system.object 'System\.Object') &#129106; [System\.Exception](https://learn.microsoft.com/en-us/dotnet/api/system.exception 'System\.Exception') &#129106; ProxyBindException
### Constructors

<a name='Proxyfan.Domain.Proxy.ProxyBindException.ProxyBindException(int,System.Net.Sockets.SocketException)'></a>

## ProxyBindException\(int, SocketException\) Constructor

The exception thrown when the proxy listener fails to bind to the configured port,
for example because the port is already in use or access is denied\.

```csharp
public ProxyBindException(int port, System.Net.Sockets.SocketException innerException);
```
#### Parameters

<a name='Proxyfan.Domain.Proxy.ProxyBindException.ProxyBindException(int,System.Net.Sockets.SocketException).port'></a>

`port` [System\.Int32](https://learn.microsoft.com/en-us/dotnet/api/system.int32 'System\.Int32')

The port number that could not be bound\.

<a name='Proxyfan.Domain.Proxy.ProxyBindException.ProxyBindException(int,System.Net.Sockets.SocketException).innerException'></a>

`innerException` [System\.Net\.Sockets\.SocketException](https://learn.microsoft.com/en-us/dotnet/api/system.net.sockets.socketexception 'System\.Net\.Sockets\.SocketException')

The underlying [System\.Net\.Sockets\.SocketException](https://learn.microsoft.com/en-us/dotnet/api/system.net.sockets.socketexception 'System\.Net\.Sockets\.SocketException') that caused the failure\.
### Properties

<a name='Proxyfan.Domain.Proxy.ProxyBindException.Port'></a>

## ProxyBindException\.Port Property

Gets the port number that could not be bound\.

```csharp
public int Port { get; }
```

#### Property Value
[System\.Int32](https://learn.microsoft.com/en-us/dotnet/api/system.int32 'System\.Int32')