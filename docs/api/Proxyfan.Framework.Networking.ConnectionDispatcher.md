### [Proxyfan\.Framework\.Networking](Proxyfan.Framework.Networking.md 'Proxyfan\.Framework\.Networking')

## ConnectionDispatcher Class

Reads the first bytes of an accepted connection to detect the protocol and dispatches
to the first registered [Proxyfan\.Domain\.Proxy\.IConnectionHandler](https://learn.microsoft.com/en-us/dotnet/api/proxyfan.domain.proxy.iconnectionhandler 'Proxyfan\.Domain\.Proxy\.IConnectionHandler') that accepts those bytes\.

```csharp
public sealed class ConnectionDispatcher
```

Inheritance [System\.Object](https://learn.microsoft.com/en-us/dotnet/api/system.object 'System\.Object') &#129106; ConnectionDispatcher

### Remarks
Use [DispatchAsync\(IProxyConnection, CancellationToken\)](Proxyfan.Framework.Networking.ConnectionDispatcher.md#Proxyfan.Framework.Networking.ConnectionDispatcher.DispatchAsync(Proxyfan.Domain.Proxy.IProxyConnection,System.Threading.CancellationToken) 'Proxyfan\.Framework\.Networking\.ConnectionDispatcher\.DispatchAsync\(Proxyfan\.Domain\.Proxy\.IProxyConnection, System\.Threading\.CancellationToken\)') as the `onConnectionAccepted` callback passed to
[Proxyfan\.Domain\.Proxy\.IProxyListener\.StartAsync\(System\.Func\{Proxyfan\.Domain\.Proxy\.IProxyConnection,System\.Threading\.CancellationToken,System\.Threading\.Tasks\.Task\},System\.Threading\.CancellationToken\)](https://learn.microsoft.com/en-us/dotnet/api/proxyfan.domain.proxy.iproxylistener.startasync#proxyfan-domain-proxy-iproxylistener-startasync(system-func{proxyfan-domain-proxy-iproxyconnection-system-threading-cancellationtoken-system-threading-tasks-task}-system-threading-cancellationtoken) 'Proxyfan\.Domain\.Proxy\.IProxyListener\.StartAsync\(System\.Func\{Proxyfan\.Domain\.Proxy\.IProxyConnection,System\.Threading\.CancellationToken,System\.Threading\.Tasks\.Task\},System\.Threading\.CancellationToken\)')\. The dispatcher peeks at up to
[PeekByteCount](Proxyfan.Framework.Networking.ConnectionDispatcher.md#Proxyfan.Framework.Networking.ConnectionDispatcher.PeekByteCount 'Proxyfan\.Framework\.Networking\.ConnectionDispatcher\.PeekByteCount') bytes without consuming them, so the handler receives the
full original byte stream from the start\.
### Constructors

<a name='Proxyfan.Framework.Networking.ConnectionDispatcher.ConnectionDispatcher(System.Collections.Generic.IEnumerable_Proxyfan.Domain.Proxy.IConnectionHandler_,Microsoft.Extensions.Options.IOptionsMonitor_Proxyfan.Domain.Proxy.ProxyOptions_,Microsoft.Extensions.Logging.ILogger_Proxyfan.Framework.Networking.ConnectionDispatcher_)'></a>

## ConnectionDispatcher\(IEnumerable\<IConnectionHandler\>, IOptionsMonitor\<ProxyOptions\>, ILogger\<ConnectionDispatcher\>\) Constructor

Reads the first bytes of an accepted connection to detect the protocol and dispatches
to the first registered [Proxyfan\.Domain\.Proxy\.IConnectionHandler](https://learn.microsoft.com/en-us/dotnet/api/proxyfan.domain.proxy.iconnectionhandler 'Proxyfan\.Domain\.Proxy\.IConnectionHandler') that accepts those bytes\.

```csharp
public ConnectionDispatcher(System.Collections.Generic.IEnumerable<Proxyfan.Domain.Proxy.IConnectionHandler> handlers, Microsoft.Extensions.Options.IOptionsMonitor<Proxyfan.Domain.Proxy.ProxyOptions> optionsMonitor, Microsoft.Extensions.Logging.ILogger<Proxyfan.Framework.Networking.ConnectionDispatcher> logger);
```
#### Parameters

<a name='Proxyfan.Framework.Networking.ConnectionDispatcher.ConnectionDispatcher(System.Collections.Generic.IEnumerable_Proxyfan.Domain.Proxy.IConnectionHandler_,Microsoft.Extensions.Options.IOptionsMonitor_Proxyfan.Domain.Proxy.ProxyOptions_,Microsoft.Extensions.Logging.ILogger_Proxyfan.Framework.Networking.ConnectionDispatcher_).handlers'></a>

`handlers` [System\.Collections\.Generic\.IEnumerable&lt;](https://learn.microsoft.com/en-us/dotnet/api/system.collections.generic.ienumerable-1 'System\.Collections\.Generic\.IEnumerable\`1')[Proxyfan\.Domain\.Proxy\.IConnectionHandler](https://learn.microsoft.com/en-us/dotnet/api/proxyfan.domain.proxy.iconnectionhandler 'Proxyfan\.Domain\.Proxy\.IConnectionHandler')[&gt;](https://learn.microsoft.com/en-us/dotnet/api/system.collections.generic.ienumerable-1 'System\.Collections\.Generic\.IEnumerable\`1')

<a name='Proxyfan.Framework.Networking.ConnectionDispatcher.ConnectionDispatcher(System.Collections.Generic.IEnumerable_Proxyfan.Domain.Proxy.IConnectionHandler_,Microsoft.Extensions.Options.IOptionsMonitor_Proxyfan.Domain.Proxy.ProxyOptions_,Microsoft.Extensions.Logging.ILogger_Proxyfan.Framework.Networking.ConnectionDispatcher_).optionsMonitor'></a>

`optionsMonitor` [Microsoft\.Extensions\.Options\.IOptionsMonitor&lt;](https://learn.microsoft.com/en-us/dotnet/api/microsoft.extensions.options.ioptionsmonitor-1 'Microsoft\.Extensions\.Options\.IOptionsMonitor\`1')[Proxyfan\.Domain\.Proxy\.ProxyOptions](https://learn.microsoft.com/en-us/dotnet/api/proxyfan.domain.proxy.proxyoptions 'Proxyfan\.Domain\.Proxy\.ProxyOptions')[&gt;](https://learn.microsoft.com/en-us/dotnet/api/microsoft.extensions.options.ioptionsmonitor-1 'Microsoft\.Extensions\.Options\.IOptionsMonitor\`1')

<a name='Proxyfan.Framework.Networking.ConnectionDispatcher.ConnectionDispatcher(System.Collections.Generic.IEnumerable_Proxyfan.Domain.Proxy.IConnectionHandler_,Microsoft.Extensions.Options.IOptionsMonitor_Proxyfan.Domain.Proxy.ProxyOptions_,Microsoft.Extensions.Logging.ILogger_Proxyfan.Framework.Networking.ConnectionDispatcher_).logger'></a>

`logger` [Microsoft\.Extensions\.Logging\.ILogger&lt;](https://learn.microsoft.com/en-us/dotnet/api/microsoft.extensions.logging.ilogger-1 'Microsoft\.Extensions\.Logging\.ILogger\`1')[ConnectionDispatcher](Proxyfan.Framework.Networking.ConnectionDispatcher.md 'Proxyfan\.Framework\.Networking\.ConnectionDispatcher')[&gt;](https://learn.microsoft.com/en-us/dotnet/api/microsoft.extensions.logging.ilogger-1 'Microsoft\.Extensions\.Logging\.ILogger\`1')

### Remarks
Use [DispatchAsync\(IProxyConnection, CancellationToken\)](Proxyfan.Framework.Networking.ConnectionDispatcher.md#Proxyfan.Framework.Networking.ConnectionDispatcher.DispatchAsync(Proxyfan.Domain.Proxy.IProxyConnection,System.Threading.CancellationToken) 'Proxyfan\.Framework\.Networking\.ConnectionDispatcher\.DispatchAsync\(Proxyfan\.Domain\.Proxy\.IProxyConnection, System\.Threading\.CancellationToken\)') as the `onConnectionAccepted` callback passed to
[Proxyfan\.Domain\.Proxy\.IProxyListener\.StartAsync\(System\.Func\{Proxyfan\.Domain\.Proxy\.IProxyConnection,System\.Threading\.CancellationToken,System\.Threading\.Tasks\.Task\},System\.Threading\.CancellationToken\)](https://learn.microsoft.com/en-us/dotnet/api/proxyfan.domain.proxy.iproxylistener.startasync#proxyfan-domain-proxy-iproxylistener-startasync(system-func{proxyfan-domain-proxy-iproxyconnection-system-threading-cancellationtoken-system-threading-tasks-task}-system-threading-cancellationtoken) 'Proxyfan\.Domain\.Proxy\.IProxyListener\.StartAsync\(System\.Func\{Proxyfan\.Domain\.Proxy\.IProxyConnection,System\.Threading\.CancellationToken,System\.Threading\.Tasks\.Task\},System\.Threading\.CancellationToken\)')\. The dispatcher peeks at up to
[PeekByteCount](Proxyfan.Framework.Networking.ConnectionDispatcher.md#Proxyfan.Framework.Networking.ConnectionDispatcher.PeekByteCount 'Proxyfan\.Framework\.Networking\.ConnectionDispatcher\.PeekByteCount') bytes without consuming them, so the handler receives the
full original byte stream from the start\.
### Fields

<a name='Proxyfan.Framework.Networking.ConnectionDispatcher.PeekByteCount'></a>

## ConnectionDispatcher\.PeekByteCount Field

The number of bytes read from the connection for protocol detection\.
Eight bytes is sufficient to identify all supported protocol signatures\.

```csharp
public const int PeekByteCount = 8;
```

#### Field Value
[System\.Int32](https://learn.microsoft.com/en-us/dotnet/api/system.int32 'System\.Int32')
### Methods

<a name='Proxyfan.Framework.Networking.ConnectionDispatcher.DispatchAsync(Proxyfan.Domain.Proxy.IProxyConnection,System.Threading.CancellationToken)'></a>

## ConnectionDispatcher\.DispatchAsync\(IProxyConnection, CancellationToken\) Method

Detects the protocol of [connection](Proxyfan.Framework.Networking.ConnectionDispatcher.md#Proxyfan.Framework.Networking.ConnectionDispatcher.DispatchAsync(Proxyfan.Domain.Proxy.IProxyConnection,System.Threading.CancellationToken).connection 'Proxyfan\.Framework\.Networking\.ConnectionDispatcher\.DispatchAsync\(Proxyfan\.Domain\.Proxy\.IProxyConnection, System\.Threading\.CancellationToken\)\.connection') from its first bytes and
dispatches to the appropriate registered handler\.

```csharp
public System.Threading.Tasks.Task DispatchAsync(Proxyfan.Domain.Proxy.IProxyConnection connection, System.Threading.CancellationToken cancellationToken);
```
#### Parameters

<a name='Proxyfan.Framework.Networking.ConnectionDispatcher.DispatchAsync(Proxyfan.Domain.Proxy.IProxyConnection,System.Threading.CancellationToken).connection'></a>

`connection` [Proxyfan\.Domain\.Proxy\.IProxyConnection](https://learn.microsoft.com/en-us/dotnet/api/proxyfan.domain.proxy.iproxyconnection 'Proxyfan\.Domain\.Proxy\.IProxyConnection')

The accepted connection to inspect and dispatch\.

<a name='Proxyfan.Framework.Networking.ConnectionDispatcher.DispatchAsync(Proxyfan.Domain.Proxy.IProxyConnection,System.Threading.CancellationToken).cancellationToken'></a>

`cancellationToken` [System\.Threading\.CancellationToken](https://learn.microsoft.com/en-us/dotnet/api/system.threading.cancellationtoken 'System\.Threading\.CancellationToken')

A token that, when cancelled, stops the dispatch\.

#### Returns
[System\.Threading\.Tasks\.Task](https://learn.microsoft.com/en-us/dotnet/api/system.threading.tasks.task 'System\.Threading\.Tasks\.Task')  
A [System\.Threading\.Tasks\.Task](https://learn.microsoft.com/en-us/dotnet/api/system.threading.tasks.task 'System\.Threading\.Tasks\.Task') that completes when the connection has been fully handled\.