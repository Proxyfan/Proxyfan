### [Proxyfan\.Framework\.Networking](Proxyfan.Framework.Networking.md 'Proxyfan\.Framework\.Networking')

## TcpProxyListener Class

A TCP proxy listener that binds to a configurable port and accepts incoming connections
asynchronously, handing each connection to a caller\-supplied callback for further processing\.

```csharp
public sealed class TcpProxyListener : Proxyfan.Domain.Proxy.IProxyListener, System.IDisposable
```

Inheritance [System\.Object](https://learn.microsoft.com/en-us/dotnet/api/system.object 'System\.Object') &#129106; TcpProxyListener

Implements [Proxyfan\.Domain\.Proxy\.IProxyListener](https://learn.microsoft.com/en-us/dotnet/api/proxyfan.domain.proxy.iproxylistener 'Proxyfan\.Domain\.Proxy\.IProxyListener'), [System\.IDisposable](https://learn.microsoft.com/en-us/dotnet/api/system.idisposable 'System\.IDisposable')
### Constructors

<a name='Proxyfan.Framework.Networking.TcpProxyListener.TcpProxyListener(Microsoft.Extensions.Options.IOptionsMonitor_Proxyfan.Domain.Proxy.ProxyOptions_,Microsoft.Extensions.Logging.ILogger_Proxyfan.Framework.Networking.TcpProxyListener_)'></a>

## TcpProxyListener\(IOptionsMonitor\<ProxyOptions\>, ILogger\<TcpProxyListener\>\) Constructor

A TCP proxy listener that binds to a configurable port and accepts incoming connections
asynchronously, handing each connection to a caller\-supplied callback for further processing\.

```csharp
public TcpProxyListener(Microsoft.Extensions.Options.IOptionsMonitor<Proxyfan.Domain.Proxy.ProxyOptions> optionsMonitor, Microsoft.Extensions.Logging.ILogger<Proxyfan.Framework.Networking.TcpProxyListener> logger);
```
#### Parameters

<a name='Proxyfan.Framework.Networking.TcpProxyListener.TcpProxyListener(Microsoft.Extensions.Options.IOptionsMonitor_Proxyfan.Domain.Proxy.ProxyOptions_,Microsoft.Extensions.Logging.ILogger_Proxyfan.Framework.Networking.TcpProxyListener_).optionsMonitor'></a>

`optionsMonitor` [Microsoft\.Extensions\.Options\.IOptionsMonitor&lt;](https://learn.microsoft.com/en-us/dotnet/api/microsoft.extensions.options.ioptionsmonitor-1 'Microsoft\.Extensions\.Options\.IOptionsMonitor\`1')[Proxyfan\.Domain\.Proxy\.ProxyOptions](https://learn.microsoft.com/en-us/dotnet/api/proxyfan.domain.proxy.proxyoptions 'Proxyfan\.Domain\.Proxy\.ProxyOptions')[&gt;](https://learn.microsoft.com/en-us/dotnet/api/microsoft.extensions.options.ioptionsmonitor-1 'Microsoft\.Extensions\.Options\.IOptionsMonitor\`1')

<a name='Proxyfan.Framework.Networking.TcpProxyListener.TcpProxyListener(Microsoft.Extensions.Options.IOptionsMonitor_Proxyfan.Domain.Proxy.ProxyOptions_,Microsoft.Extensions.Logging.ILogger_Proxyfan.Framework.Networking.TcpProxyListener_).logger'></a>

`logger` [Microsoft\.Extensions\.Logging\.ILogger&lt;](https://learn.microsoft.com/en-us/dotnet/api/microsoft.extensions.logging.ilogger-1 'Microsoft\.Extensions\.Logging\.ILogger\`1')[TcpProxyListener](Proxyfan.Framework.Networking.TcpProxyListener.md 'Proxyfan\.Framework\.Networking\.TcpProxyListener')[&gt;](https://learn.microsoft.com/en-us/dotnet/api/microsoft.extensions.logging.ilogger-1 'Microsoft\.Extensions\.Logging\.ILogger\`1')
### Properties

<a name='Proxyfan.Framework.Networking.TcpProxyListener.BoundPort'></a>

## TcpProxyListener\.BoundPort Property

Gets the port number the listener is actually bound to, or [null](https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/null 'https://docs\.microsoft\.com/en\-us/dotnet/csharp/language\-reference/keywords/null')
if the listener is not currently active\.

```csharp
public System.Nullable<int> BoundPort { get; }
```

Implements [BoundPort](https://learn.microsoft.com/en-us/dotnet/api/proxyfan.domain.proxy.iproxylistener.boundport 'Proxyfan\.Domain\.Proxy\.IProxyListener\.BoundPort')

#### Property Value
[System\.Nullable&lt;](https://learn.microsoft.com/en-us/dotnet/api/system.nullable-1 'System\.Nullable\`1')[System\.Int32](https://learn.microsoft.com/en-us/dotnet/api/system.int32 'System\.Int32')[&gt;](https://learn.microsoft.com/en-us/dotnet/api/system.nullable-1 'System\.Nullable\`1')

<a name='Proxyfan.Framework.Networking.TcpProxyListener.IsListening'></a>

## TcpProxyListener\.IsListening Property

Gets a value indicating whether the listener is currently bound and accepting connections\.

```csharp
public bool IsListening { get; }
```

Implements [IsListening](https://learn.microsoft.com/en-us/dotnet/api/proxyfan.domain.proxy.iproxylistener.islistening 'Proxyfan\.Domain\.Proxy\.IProxyListener\.IsListening')

#### Property Value
[System\.Boolean](https://learn.microsoft.com/en-us/dotnet/api/system.boolean 'System\.Boolean')
### Methods

<a name='Proxyfan.Framework.Networking.TcpProxyListener.Dispose()'></a>

## TcpProxyListener\.Dispose\(\) Method

Releases all resources held by this listener\.

```csharp
public void Dispose();
```

Implements [Dispose\(\)](https://learn.microsoft.com/en-us/dotnet/api/system.idisposable.dispose 'System\.IDisposable\.Dispose')

<a name='Proxyfan.Framework.Networking.TcpProxyListener.StartAsync(System.Func_Proxyfan.Domain.Proxy.IProxyConnection,System.Threading.CancellationToken,System.Threading.Tasks.Task_,System.Threading.CancellationToken)'></a>

## TcpProxyListener\.StartAsync\(Func\<IProxyConnection,CancellationToken,Task\>, CancellationToken\) Method

Starts the listener, binds to the configured port, and begins accepting incoming connections\.
Each accepted connection is delivered to [onConnectionAccepted](Proxyfan.Framework.Networking.TcpProxyListener.md#Proxyfan.Framework.Networking.TcpProxyListener.StartAsync(System.Func_Proxyfan.Domain.Proxy.IProxyConnection,System.Threading.CancellationToken,System.Threading.Tasks.Task_,System.Threading.CancellationToken).onConnectionAccepted 'Proxyfan\.Framework\.Networking\.TcpProxyListener\.StartAsync\(System\.Func\<Proxyfan\.Domain\.Proxy\.IProxyConnection,System\.Threading\.CancellationToken,System\.Threading\.Tasks\.Task\>, System\.Threading\.CancellationToken\)\.onConnectionAccepted')\.

```csharp
public System.Threading.Tasks.Task StartAsync(System.Func<Proxyfan.Domain.Proxy.IProxyConnection,System.Threading.CancellationToken,System.Threading.Tasks.Task> onConnectionAccepted, System.Threading.CancellationToken cancellationToken);
```
#### Parameters

<a name='Proxyfan.Framework.Networking.TcpProxyListener.StartAsync(System.Func_Proxyfan.Domain.Proxy.IProxyConnection,System.Threading.CancellationToken,System.Threading.Tasks.Task_,System.Threading.CancellationToken).onConnectionAccepted'></a>

`onConnectionAccepted` [System\.Func&lt;](https://learn.microsoft.com/en-us/dotnet/api/system.func-3 'System\.Func\`3')[Proxyfan\.Domain\.Proxy\.IProxyConnection](https://learn.microsoft.com/en-us/dotnet/api/proxyfan.domain.proxy.iproxyconnection 'Proxyfan\.Domain\.Proxy\.IProxyConnection')[,](https://learn.microsoft.com/en-us/dotnet/api/system.func-3 'System\.Func\`3')[System\.Threading\.CancellationToken](https://learn.microsoft.com/en-us/dotnet/api/system.threading.cancellationtoken 'System\.Threading\.CancellationToken')[,](https://learn.microsoft.com/en-us/dotnet/api/system.func-3 'System\.Func\`3')[System\.Threading\.Tasks\.Task](https://learn.microsoft.com/en-us/dotnet/api/system.threading.tasks.task 'System\.Threading\.Tasks\.Task')[&gt;](https://learn.microsoft.com/en-us/dotnet/api/system.func-3 'System\.Func\`3')

An asynchronous callback invoked for each accepted connection\.
The connection is passed as the first argument; the cancellation token reflects the listener lifetime\.

<a name='Proxyfan.Framework.Networking.TcpProxyListener.StartAsync(System.Func_Proxyfan.Domain.Proxy.IProxyConnection,System.Threading.CancellationToken,System.Threading.Tasks.Task_,System.Threading.CancellationToken).cancellationToken'></a>

`cancellationToken` [System\.Threading\.CancellationToken](https://learn.microsoft.com/en-us/dotnet/api/system.threading.cancellationtoken 'System\.Threading\.CancellationToken')

A token that, when cancelled, stops the listener gracefully\.

Implements [StartAsync\(Func&lt;IProxyConnection,CancellationToken,Task&gt;, CancellationToken\)](https://learn.microsoft.com/en-us/dotnet/api/proxyfan.domain.proxy.iproxylistener.startasync#proxyfan-domain-proxy-iproxylistener-startasync(system-func{proxyfan-domain-proxy-iproxyconnection-system-threading-cancellationtoken-system-threading-tasks-task}-system-threading-cancellationtoken) 'Proxyfan\.Domain\.Proxy\.IProxyListener\.StartAsync\(System\.Func\{Proxyfan\.Domain\.Proxy\.IProxyConnection,System\.Threading\.CancellationToken,System\.Threading\.Tasks\.Task\},System\.Threading\.CancellationToken\)')

#### Returns
[System\.Threading\.Tasks\.Task](https://learn.microsoft.com/en-us/dotnet/api/system.threading.tasks.task 'System\.Threading\.Tasks\.Task')  
A [System\.Threading\.Tasks\.Task](https://learn.microsoft.com/en-us/dotnet/api/system.threading.tasks.task 'System\.Threading\.Tasks\.Task') that completes when the listener has bound and the accept loop is running\.

#### Exceptions

[Proxyfan\.Domain\.Proxy\.ProxyBindException](https://learn.microsoft.com/en-us/dotnet/api/proxyfan.domain.proxy.proxybindexception 'Proxyfan\.Domain\.Proxy\.ProxyBindException')  
Thrown when the configured port is already in use or access is denied\.

<a name='Proxyfan.Framework.Networking.TcpProxyListener.StopAsync(System.Threading.CancellationToken)'></a>

## TcpProxyListener\.StopAsync\(CancellationToken\) Method

Stops the listener gracefully, allowing in\-flight connection handling to complete
within the scope of [cancellationToken](Proxyfan.Framework.Networking.TcpProxyListener.md#Proxyfan.Framework.Networking.TcpProxyListener.StopAsync(System.Threading.CancellationToken).cancellationToken 'Proxyfan\.Framework\.Networking\.TcpProxyListener\.StopAsync\(System\.Threading\.CancellationToken\)\.cancellationToken')\.

```csharp
public System.Threading.Tasks.Task StopAsync(System.Threading.CancellationToken cancellationToken);
```
#### Parameters

<a name='Proxyfan.Framework.Networking.TcpProxyListener.StopAsync(System.Threading.CancellationToken).cancellationToken'></a>

`cancellationToken` [System\.Threading\.CancellationToken](https://learn.microsoft.com/en-us/dotnet/api/system.threading.cancellationtoken 'System\.Threading\.CancellationToken')

A token that forces an immediate stop if cancelled\.

Implements [StopAsync\(CancellationToken\)](https://learn.microsoft.com/en-us/dotnet/api/proxyfan.domain.proxy.iproxylistener.stopasync#proxyfan-domain-proxy-iproxylistener-stopasync(system-threading-cancellationtoken) 'Proxyfan\.Domain\.Proxy\.IProxyListener\.StopAsync\(System\.Threading\.CancellationToken\)')

#### Returns
[System\.Threading\.Tasks\.Task](https://learn.microsoft.com/en-us/dotnet/api/system.threading.tasks.task 'System\.Threading\.Tasks\.Task')  
A [System\.Threading\.Tasks\.Task](https://learn.microsoft.com/en-us/dotnet/api/system.threading.tasks.task 'System\.Threading\.Tasks\.Task') that completes when the listener has fully stopped\.