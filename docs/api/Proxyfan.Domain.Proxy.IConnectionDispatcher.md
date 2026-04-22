#### [Domain\.Proxy](index.md 'index')
### [Proxyfan\.Domain\.Proxy](Proxyfan.Domain.Proxy.md 'Proxyfan\.Domain\.Proxy')

## IConnectionDispatcher Interface

Defines the contract for a component that accepts an incoming connection,
detects its protocol, and routes it to the appropriate handler\.

```csharp
public interface IConnectionDispatcher
```
### Methods

<a name='Proxyfan.Domain.Proxy.IConnectionDispatcher.DispatchAsync(Proxyfan.Domain.Proxy.IProxyConnection,System.Threading.CancellationToken)'></a>

## IConnectionDispatcher\.DispatchAsync\(IProxyConnection, CancellationToken\) Method

Detects the protocol of [connection](Proxyfan.Domain.Proxy.IConnectionDispatcher.md#Proxyfan.Domain.Proxy.IConnectionDispatcher.DispatchAsync(Proxyfan.Domain.Proxy.IProxyConnection,System.Threading.CancellationToken).connection 'Proxyfan\.Domain\.Proxy\.IConnectionDispatcher\.DispatchAsync\(Proxyfan\.Domain\.Proxy\.IProxyConnection, System\.Threading\.CancellationToken\)\.connection') from its first bytes and
dispatches to the appropriate registered handler\.

```csharp
System.Threading.Tasks.Task DispatchAsync(Proxyfan.Domain.Proxy.IProxyConnection connection, System.Threading.CancellationToken cancellationToken);
```
#### Parameters

<a name='Proxyfan.Domain.Proxy.IConnectionDispatcher.DispatchAsync(Proxyfan.Domain.Proxy.IProxyConnection,System.Threading.CancellationToken).connection'></a>

`connection` [IProxyConnection](Proxyfan.Domain.Proxy.IProxyConnection.md 'Proxyfan\.Domain\.Proxy\.IProxyConnection')

The accepted connection to inspect and dispatch\.

<a name='Proxyfan.Domain.Proxy.IConnectionDispatcher.DispatchAsync(Proxyfan.Domain.Proxy.IProxyConnection,System.Threading.CancellationToken).cancellationToken'></a>

`cancellationToken` [System\.Threading\.CancellationToken](https://learn.microsoft.com/en-us/dotnet/api/system.threading.cancellationtoken 'System\.Threading\.CancellationToken')

A token that, when cancelled, stops the dispatch\.

#### Returns
[System\.Threading\.Tasks\.Task](https://learn.microsoft.com/en-us/dotnet/api/system.threading.tasks.task 'System\.Threading\.Tasks\.Task')  
A [System\.Threading\.Tasks\.Task](https://learn.microsoft.com/en-us/dotnet/api/system.threading.tasks.task 'System\.Threading\.Tasks\.Task') that completes when the connection has been fully handled\.