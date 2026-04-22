#### [Domain\.Proxy](index.md 'index')
### [Proxyfan\.Domain\.Proxy](Proxyfan.Domain.Proxy.md 'Proxyfan\.Domain\.Proxy')

## ProxyServer Class

Aggregate root that manages the complete proxy server lifecycle: configuration,
start, stop, restart, and status reporting\.

```csharp
public sealed class ProxyServer : System.IAsyncDisposable
```

Inheritance [System\.Object](https://learn.microsoft.com/en-us/dotnet/api/system.object 'System\.Object') &#129106; ProxyServer

Implements [System\.IAsyncDisposable](https://learn.microsoft.com/en-us/dotnet/api/system.iasyncdisposable 'System\.IAsyncDisposable')

### Remarks

[ProxyServer](Proxyfan.Domain.Proxy.ProxyServer.md 'Proxyfan\.Domain\.Proxy\.ProxyServer') coordinates the [IProxyListener](Proxyfan.Domain.Proxy.IProxyListener.md 'Proxyfan\.Domain\.Proxy\.IProxyListener') abstraction
                    and publishes domain events for lifecycle changes via [Proxyfan\.Domain\.IDomainEventBus](https://learn.microsoft.com/en-us/dotnet/api/proxyfan.domain.idomaineventbus 'Proxyfan\.Domain\.IDomainEventBus').
                    Both the UI and CLI interact exclusively with this class to control the proxy.

If [AutoStart](Proxyfan.Domain.Proxy.ProxyOptions.md#Proxyfan.Domain.Proxy.ProxyOptions.AutoStart 'Proxyfan\.Domain\.Proxy\.ProxyOptions\.AutoStart') is [true](https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/builtin-types/bool 'https://docs\.microsoft\.com/en\-us/dotnet/csharp/language\-reference/builtin\-types/bool') when the server is
constructed, [StartAsync\(CancellationToken\)](Proxyfan.Domain.Proxy.ProxyServer.md#Proxyfan.Domain.Proxy.ProxyServer.StartAsync(System.Threading.CancellationToken) 'Proxyfan\.Domain\.Proxy\.ProxyServer\.StartAsync\(System\.Threading\.CancellationToken\)') is fired asynchronously in
the background. Errors during auto-start transition the server to
[Faulted](Proxyfan.Domain.Proxy.ProxyStatus.md#Proxyfan.Domain.Proxy.ProxyStatus.Faulted 'Proxyfan\.Domain\.Proxy\.ProxyStatus\.Faulted').

Configuration changes detected via [Microsoft\.Extensions\.Options\.IOptionsMonitor&lt;&gt;](https://learn.microsoft.com/en-us/dotnet/api/microsoft.extensions.options.ioptionsmonitor-1 'Microsoft\.Extensions\.Options\.IOptionsMonitor\`1') automatically
trigger a restart when the server is running. Dispose stops the server and releases all
resources.
### Constructors

<a name='Proxyfan.Domain.Proxy.ProxyServer.ProxyServer(Proxyfan.Domain.Proxy.IProxyListener,Proxyfan.Domain.Proxy.IConnectionDispatcher,Microsoft.Extensions.Options.IOptionsMonitor_Proxyfan.Domain.Proxy.ProxyOptions_,Proxyfan.Domain.IDomainEventBus,Microsoft.Extensions.Logging.ILogger_Proxyfan.Domain.Proxy.ProxyServer_)'></a>

## ProxyServer\(IProxyListener, IConnectionDispatcher, IOptionsMonitor\<ProxyOptions\>, IDomainEventBus, ILogger\<ProxyServer\>\) Constructor

Initializes a new [ProxyServer](Proxyfan.Domain.Proxy.ProxyServer.md 'Proxyfan\.Domain\.Proxy\.ProxyServer') and, if
[AutoStart](Proxyfan.Domain.Proxy.ProxyOptions.md#Proxyfan.Domain.Proxy.ProxyOptions.AutoStart 'Proxyfan\.Domain\.Proxy\.ProxyOptions\.AutoStart') is enabled, begins listening asynchronously\.

```csharp
public ProxyServer(Proxyfan.Domain.Proxy.IProxyListener listener, Proxyfan.Domain.Proxy.IConnectionDispatcher dispatcher, Microsoft.Extensions.Options.IOptionsMonitor<Proxyfan.Domain.Proxy.ProxyOptions> optionsMonitor, Proxyfan.Domain.IDomainEventBus eventBus, Microsoft.Extensions.Logging.ILogger<Proxyfan.Domain.Proxy.ProxyServer> logger);
```
#### Parameters

<a name='Proxyfan.Domain.Proxy.ProxyServer.ProxyServer(Proxyfan.Domain.Proxy.IProxyListener,Proxyfan.Domain.Proxy.IConnectionDispatcher,Microsoft.Extensions.Options.IOptionsMonitor_Proxyfan.Domain.Proxy.ProxyOptions_,Proxyfan.Domain.IDomainEventBus,Microsoft.Extensions.Logging.ILogger_Proxyfan.Domain.Proxy.ProxyServer_).listener'></a>

`listener` [IProxyListener](Proxyfan.Domain.Proxy.IProxyListener.md 'Proxyfan\.Domain\.Proxy\.IProxyListener')

The TCP proxy listener to delegate to\.

<a name='Proxyfan.Domain.Proxy.ProxyServer.ProxyServer(Proxyfan.Domain.Proxy.IProxyListener,Proxyfan.Domain.Proxy.IConnectionDispatcher,Microsoft.Extensions.Options.IOptionsMonitor_Proxyfan.Domain.Proxy.ProxyOptions_,Proxyfan.Domain.IDomainEventBus,Microsoft.Extensions.Logging.ILogger_Proxyfan.Domain.Proxy.ProxyServer_).dispatcher'></a>

`dispatcher` [IConnectionDispatcher](Proxyfan.Domain.Proxy.IConnectionDispatcher.md 'Proxyfan\.Domain\.Proxy\.IConnectionDispatcher')

The connection dispatcher that handles accepted connections\.

<a name='Proxyfan.Domain.Proxy.ProxyServer.ProxyServer(Proxyfan.Domain.Proxy.IProxyListener,Proxyfan.Domain.Proxy.IConnectionDispatcher,Microsoft.Extensions.Options.IOptionsMonitor_Proxyfan.Domain.Proxy.ProxyOptions_,Proxyfan.Domain.IDomainEventBus,Microsoft.Extensions.Logging.ILogger_Proxyfan.Domain.Proxy.ProxyServer_).optionsMonitor'></a>

`optionsMonitor` [Microsoft\.Extensions\.Options\.IOptionsMonitor&lt;](https://learn.microsoft.com/en-us/dotnet/api/microsoft.extensions.options.ioptionsmonitor-1 'Microsoft\.Extensions\.Options\.IOptionsMonitor\`1')[ProxyOptions](Proxyfan.Domain.Proxy.ProxyOptions.md 'Proxyfan\.Domain\.Proxy\.ProxyOptions')[&gt;](https://learn.microsoft.com/en-us/dotnet/api/microsoft.extensions.options.ioptionsmonitor-1 'Microsoft\.Extensions\.Options\.IOptionsMonitor\`1')

Live options monitor for [ProxyOptions](Proxyfan.Domain.Proxy.ProxyOptions.md 'Proxyfan\.Domain\.Proxy\.ProxyOptions')\.

<a name='Proxyfan.Domain.Proxy.ProxyServer.ProxyServer(Proxyfan.Domain.Proxy.IProxyListener,Proxyfan.Domain.Proxy.IConnectionDispatcher,Microsoft.Extensions.Options.IOptionsMonitor_Proxyfan.Domain.Proxy.ProxyOptions_,Proxyfan.Domain.IDomainEventBus,Microsoft.Extensions.Logging.ILogger_Proxyfan.Domain.Proxy.ProxyServer_).eventBus'></a>

`eventBus` [Proxyfan\.Domain\.IDomainEventBus](https://learn.microsoft.com/en-us/dotnet/api/proxyfan.domain.idomaineventbus 'Proxyfan\.Domain\.IDomainEventBus')

Domain event bus for publishing lifecycle events\.

<a name='Proxyfan.Domain.Proxy.ProxyServer.ProxyServer(Proxyfan.Domain.Proxy.IProxyListener,Proxyfan.Domain.Proxy.IConnectionDispatcher,Microsoft.Extensions.Options.IOptionsMonitor_Proxyfan.Domain.Proxy.ProxyOptions_,Proxyfan.Domain.IDomainEventBus,Microsoft.Extensions.Logging.ILogger_Proxyfan.Domain.Proxy.ProxyServer_).logger'></a>

`logger` [Microsoft\.Extensions\.Logging\.ILogger&lt;](https://learn.microsoft.com/en-us/dotnet/api/microsoft.extensions.logging.ilogger-1 'Microsoft\.Extensions\.Logging\.ILogger\`1')[ProxyServer](Proxyfan.Domain.Proxy.ProxyServer.md 'Proxyfan\.Domain\.Proxy\.ProxyServer')[&gt;](https://learn.microsoft.com/en-us/dotnet/api/microsoft.extensions.logging.ilogger-1 'Microsoft\.Extensions\.Logging\.ILogger\`1')

Logger for structured diagnostic output\.
### Properties

<a name='Proxyfan.Domain.Proxy.ProxyServer.BoundPort'></a>

## ProxyServer\.BoundPort Property

Gets the port number the listener is currently bound to, or [null](https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/null 'https://docs\.microsoft\.com/en\-us/dotnet/csharp/language\-reference/keywords/null')
when the server is not running\.

```csharp
public System.Nullable<int> BoundPort { get; }
```

#### Property Value
[System\.Nullable&lt;](https://learn.microsoft.com/en-us/dotnet/api/system.nullable-1 'System\.Nullable\`1')[System\.Int32](https://learn.microsoft.com/en-us/dotnet/api/system.int32 'System\.Int32')[&gt;](https://learn.microsoft.com/en-us/dotnet/api/system.nullable-1 'System\.Nullable\`1')

<a name='Proxyfan.Domain.Proxy.ProxyServer.Status'></a>

## ProxyServer\.Status Property

Gets the current lifecycle status of the proxy server\.

```csharp
public Proxyfan.Domain.Proxy.ProxyStatus Status { get; }
```

#### Property Value
[ProxyStatus](Proxyfan.Domain.Proxy.ProxyStatus.md 'Proxyfan\.Domain\.Proxy\.ProxyStatus')
### Methods

<a name='Proxyfan.Domain.Proxy.ProxyServer.RestartAsync(System.Threading.CancellationToken)'></a>

## ProxyServer\.RestartAsync\(CancellationToken\) Method

Restarts the proxy server atomically under a single lifecycle lock\. If the server
is not running, this is equivalent to [StartAsync\(CancellationToken\)](Proxyfan.Domain.Proxy.ProxyServer.md#Proxyfan.Domain.Proxy.ProxyServer.StartAsync(System.Threading.CancellationToken) 'Proxyfan\.Domain\.Proxy\.ProxyServer\.StartAsync\(System\.Threading\.CancellationToken\)')\.

```csharp
public System.Threading.Tasks.Task<Proxyfan.Domain.Result> RestartAsync(System.Threading.CancellationToken cancellationToken=default(System.Threading.CancellationToken));
```
#### Parameters

<a name='Proxyfan.Domain.Proxy.ProxyServer.RestartAsync(System.Threading.CancellationToken).cancellationToken'></a>

`cancellationToken` [System\.Threading\.CancellationToken](https://learn.microsoft.com/en-us/dotnet/api/system.threading.cancellationtoken 'System\.Threading\.CancellationToken')

A token that cancels the restart operation\.

#### Returns
[System\.Threading\.Tasks\.Task&lt;](https://learn.microsoft.com/en-us/dotnet/api/system.threading.tasks.task-1 'System\.Threading\.Tasks\.Task\`1')[Proxyfan\.Domain\.Result](https://learn.microsoft.com/en-us/dotnet/api/proxyfan.domain.result 'Proxyfan\.Domain\.Result')[&gt;](https://learn.microsoft.com/en-us/dotnet/api/system.threading.tasks.task-1 'System\.Threading\.Tasks\.Task\`1')  
[Proxyfan\.Domain\.Result\.Success](https://learn.microsoft.com/en-us/dotnet/api/proxyfan.domain.result.success 'Proxyfan\.Domain\.Result\.Success') when the server is listening again;
                [Proxyfan\.Domain\.Result\.Failure\(Proxyfan\.Domain\.DomainError\)](https://learn.microsoft.com/en-us/dotnet/api/proxyfan.domain.result.failure#proxyfan-domain-result-failure(proxyfan-domain-domainerror) 'Proxyfan\.Domain\.Result\.Failure\(Proxyfan\.Domain\.DomainError\)') on failure\.

<a name='Proxyfan.Domain.Proxy.ProxyServer.StartAsync(System.Threading.CancellationToken)'></a>

## ProxyServer\.StartAsync\(CancellationToken\) Method

Starts the proxy server\. If the server is already [Running](Proxyfan.Domain.Proxy.ProxyStatus.md#Proxyfan.Domain.Proxy.ProxyStatus.Running 'Proxyfan\.Domain\.Proxy\.ProxyStatus\.Running')
or [Starting](Proxyfan.Domain.Proxy.ProxyStatus.md#Proxyfan.Domain.Proxy.ProxyStatus.Starting 'Proxyfan\.Domain\.Proxy\.ProxyStatus\.Starting'), the call is a no\-op and returns success\.

```csharp
public System.Threading.Tasks.Task<Proxyfan.Domain.Result> StartAsync(System.Threading.CancellationToken cancellationToken=default(System.Threading.CancellationToken));
```
#### Parameters

<a name='Proxyfan.Domain.Proxy.ProxyServer.StartAsync(System.Threading.CancellationToken).cancellationToken'></a>

`cancellationToken` [System\.Threading\.CancellationToken](https://learn.microsoft.com/en-us/dotnet/api/system.threading.cancellationtoken 'System\.Threading\.CancellationToken')

A token that cancels the start operation\.

#### Returns
[System\.Threading\.Tasks\.Task&lt;](https://learn.microsoft.com/en-us/dotnet/api/system.threading.tasks.task-1 'System\.Threading\.Tasks\.Task\`1')[Proxyfan\.Domain\.Result](https://learn.microsoft.com/en-us/dotnet/api/proxyfan.domain.result 'Proxyfan\.Domain\.Result')[&gt;](https://learn.microsoft.com/en-us/dotnet/api/system.threading.tasks.task-1 'System\.Threading\.Tasks\.Task\`1')  
[Proxyfan\.Domain\.Result\.Success](https://learn.microsoft.com/en-us/dotnet/api/proxyfan.domain.result.success 'Proxyfan\.Domain\.Result\.Success') when the server is listening;
                [Proxyfan\.Domain\.Result\.Failure\(Proxyfan\.Domain\.DomainError\)](https://learn.microsoft.com/en-us/dotnet/api/proxyfan.domain.result.failure#proxyfan-domain-result-failure(proxyfan-domain-domainerror) 'Proxyfan\.Domain\.Result\.Failure\(Proxyfan\.Domain\.DomainError\)') with a [ProxyBindError](Proxyfan.Domain.Proxy.ProxyBindError.md 'Proxyfan\.Domain\.Proxy\.ProxyBindError') or
                [ProxyFaultedError](Proxyfan.Domain.Proxy.ProxyFaultedError.md 'Proxyfan\.Domain\.Proxy\.ProxyFaultedError') on failure\.

<a name='Proxyfan.Domain.Proxy.ProxyServer.StopAsync(System.Threading.CancellationToken)'></a>

## ProxyServer\.StopAsync\(CancellationToken\) Method

Stops the proxy server gracefully\. If the server is already
[Stopped](Proxyfan.Domain.Proxy.ProxyStatus.md#Proxyfan.Domain.Proxy.ProxyStatus.Stopped 'Proxyfan\.Domain\.Proxy\.ProxyStatus\.Stopped') or [Stopping](Proxyfan.Domain.Proxy.ProxyStatus.md#Proxyfan.Domain.Proxy.ProxyStatus.Stopping 'Proxyfan\.Domain\.Proxy\.ProxyStatus\.Stopping'), the
call is a no\-op and returns success\.

```csharp
public System.Threading.Tasks.Task<Proxyfan.Domain.Result> StopAsync(System.Threading.CancellationToken cancellationToken=default(System.Threading.CancellationToken));
```
#### Parameters

<a name='Proxyfan.Domain.Proxy.ProxyServer.StopAsync(System.Threading.CancellationToken).cancellationToken'></a>

`cancellationToken` [System\.Threading\.CancellationToken](https://learn.microsoft.com/en-us/dotnet/api/system.threading.cancellationtoken 'System\.Threading\.CancellationToken')

A token that forces an immediate stop if cancelled\.

#### Returns
[System\.Threading\.Tasks\.Task&lt;](https://learn.microsoft.com/en-us/dotnet/api/system.threading.tasks.task-1 'System\.Threading\.Tasks\.Task\`1')[Proxyfan\.Domain\.Result](https://learn.microsoft.com/en-us/dotnet/api/proxyfan.domain.result 'Proxyfan\.Domain\.Result')[&gt;](https://learn.microsoft.com/en-us/dotnet/api/system.threading.tasks.task-1 'System\.Threading\.Tasks\.Task\`1')  
[Proxyfan\.Domain\.Result\.Success](https://learn.microsoft.com/en-us/dotnet/api/proxyfan.domain.result.success 'Proxyfan\.Domain\.Result\.Success') when the server has stopped;
                [Proxyfan\.Domain\.Result\.Failure\(Proxyfan\.Domain\.DomainError\)](https://learn.microsoft.com/en-us/dotnet/api/proxyfan.domain.result.failure#proxyfan-domain-result-failure(proxyfan-domain-domainerror) 'Proxyfan\.Domain\.Result\.Failure\(Proxyfan\.Domain\.DomainError\)') with a [ProxyFaultedError](Proxyfan.Domain.Proxy.ProxyFaultedError.md 'Proxyfan\.Domain\.Proxy\.ProxyFaultedError')
                on unexpected failure\.