#### [Domain\.Proxy](index.md 'index')
### [Proxyfan\.Domain\.Proxy](Proxyfan.Domain.Proxy.md 'Proxyfan\.Domain\.Proxy')

## IProxyListener Interface

Defines the lifecycle contract for a TCP proxy listener that binds to a port,
accepts incoming connections, and dispatches each connection via a callback\.

```csharp
public interface IProxyListener
```
### Properties

<a name='Proxyfan.Domain.Proxy.IProxyListener.BoundPort'></a>

## IProxyListener\.BoundPort Property

Gets the port number the listener is actually bound to, or [null](https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/null 'https://docs\.microsoft\.com/en\-us/dotnet/csharp/language\-reference/keywords/null')
if the listener is not currently active\.

```csharp
System.Nullable<int> BoundPort { get; }
```

#### Property Value
[System\.Nullable&lt;](https://learn.microsoft.com/en-us/dotnet/api/system.nullable-1 'System\.Nullable\`1')[System\.Int32](https://learn.microsoft.com/en-us/dotnet/api/system.int32 'System\.Int32')[&gt;](https://learn.microsoft.com/en-us/dotnet/api/system.nullable-1 'System\.Nullable\`1')

<a name='Proxyfan.Domain.Proxy.IProxyListener.IsListening'></a>

## IProxyListener\.IsListening Property

Gets a value indicating whether the listener is currently bound and accepting connections\.

```csharp
bool IsListening { get; }
```

#### Property Value
[System\.Boolean](https://learn.microsoft.com/en-us/dotnet/api/system.boolean 'System\.Boolean')
### Methods

<a name='Proxyfan.Domain.Proxy.IProxyListener.StartAsync(System.Func_Proxyfan.Domain.Proxy.IProxyConnection,System.Threading.CancellationToken,System.Threading.Tasks.Task_,System.Threading.CancellationToken)'></a>

## IProxyListener\.StartAsync\(Func\<IProxyConnection,CancellationToken,Task\>, CancellationToken\) Method

Starts the listener, binds to the configured port, and begins accepting incoming connections\.
Each accepted connection is delivered to [onConnectionAccepted](Proxyfan.Domain.Proxy.IProxyListener.md#Proxyfan.Domain.Proxy.IProxyListener.StartAsync(System.Func_Proxyfan.Domain.Proxy.IProxyConnection,System.Threading.CancellationToken,System.Threading.Tasks.Task_,System.Threading.CancellationToken).onConnectionAccepted 'Proxyfan\.Domain\.Proxy\.IProxyListener\.StartAsync\(System\.Func\<Proxyfan\.Domain\.Proxy\.IProxyConnection,System\.Threading\.CancellationToken,System\.Threading\.Tasks\.Task\>, System\.Threading\.CancellationToken\)\.onConnectionAccepted')\.

```csharp
System.Threading.Tasks.Task StartAsync(System.Func<Proxyfan.Domain.Proxy.IProxyConnection,System.Threading.CancellationToken,System.Threading.Tasks.Task> onConnectionAccepted, System.Threading.CancellationToken cancellationToken);
```
#### Parameters

<a name='Proxyfan.Domain.Proxy.IProxyListener.StartAsync(System.Func_Proxyfan.Domain.Proxy.IProxyConnection,System.Threading.CancellationToken,System.Threading.Tasks.Task_,System.Threading.CancellationToken).onConnectionAccepted'></a>

`onConnectionAccepted` [System\.Func&lt;](https://learn.microsoft.com/en-us/dotnet/api/system.func-3 'System\.Func\`3')[IProxyConnection](Proxyfan.Domain.Proxy.IProxyConnection.md 'Proxyfan\.Domain\.Proxy\.IProxyConnection')[,](https://learn.microsoft.com/en-us/dotnet/api/system.func-3 'System\.Func\`3')[System\.Threading\.CancellationToken](https://learn.microsoft.com/en-us/dotnet/api/system.threading.cancellationtoken 'System\.Threading\.CancellationToken')[,](https://learn.microsoft.com/en-us/dotnet/api/system.func-3 'System\.Func\`3')[System\.Threading\.Tasks\.Task](https://learn.microsoft.com/en-us/dotnet/api/system.threading.tasks.task 'System\.Threading\.Tasks\.Task')[&gt;](https://learn.microsoft.com/en-us/dotnet/api/system.func-3 'System\.Func\`3')

An asynchronous callback invoked for each accepted connection\.
The connection is passed as the first argument; the cancellation token reflects the listener lifetime\.

<a name='Proxyfan.Domain.Proxy.IProxyListener.StartAsync(System.Func_Proxyfan.Domain.Proxy.IProxyConnection,System.Threading.CancellationToken,System.Threading.Tasks.Task_,System.Threading.CancellationToken).cancellationToken'></a>

`cancellationToken` [System\.Threading\.CancellationToken](https://learn.microsoft.com/en-us/dotnet/api/system.threading.cancellationtoken 'System\.Threading\.CancellationToken')

A token that, when cancelled, stops the listener gracefully\.

#### Returns
[System\.Threading\.Tasks\.Task](https://learn.microsoft.com/en-us/dotnet/api/system.threading.tasks.task 'System\.Threading\.Tasks\.Task')  
A [System\.Threading\.Tasks\.Task](https://learn.microsoft.com/en-us/dotnet/api/system.threading.tasks.task 'System\.Threading\.Tasks\.Task') that completes when the listener has bound and the accept loop is running\.

#### Exceptions

[ProxyBindException](Proxyfan.Domain.Proxy.ProxyBindException.md 'Proxyfan\.Domain\.Proxy\.ProxyBindException')  
Thrown when the configured port is already in use or access is denied\.

<a name='Proxyfan.Domain.Proxy.IProxyListener.StopAsync(System.Threading.CancellationToken)'></a>

## IProxyListener\.StopAsync\(CancellationToken\) Method

Stops the listener gracefully, allowing in\-flight connection handling to complete
within the scope of [cancellationToken](Proxyfan.Domain.Proxy.IProxyListener.md#Proxyfan.Domain.Proxy.IProxyListener.StopAsync(System.Threading.CancellationToken).cancellationToken 'Proxyfan\.Domain\.Proxy\.IProxyListener\.StopAsync\(System\.Threading\.CancellationToken\)\.cancellationToken')\.

```csharp
System.Threading.Tasks.Task StopAsync(System.Threading.CancellationToken cancellationToken);
```
#### Parameters

<a name='Proxyfan.Domain.Proxy.IProxyListener.StopAsync(System.Threading.CancellationToken).cancellationToken'></a>

`cancellationToken` [System\.Threading\.CancellationToken](https://learn.microsoft.com/en-us/dotnet/api/system.threading.cancellationtoken 'System\.Threading\.CancellationToken')

A token that forces an immediate stop if cancelled\.

#### Returns
[System\.Threading\.Tasks\.Task](https://learn.microsoft.com/en-us/dotnet/api/system.threading.tasks.task 'System\.Threading\.Tasks\.Task')  
A [System\.Threading\.Tasks\.Task](https://learn.microsoft.com/en-us/dotnet/api/system.threading.tasks.task 'System\.Threading\.Tasks\.Task') that completes when the listener has fully stopped\.