### [Proxyfan\.Domain\.Proxy](Proxyfan.Domain.Proxy.md 'Proxyfan\.Domain\.Proxy')

## IConnectionHandler Interface

Defines the contract for a component that handles an incoming proxy connection
for a specific protocol\.

```csharp
public interface IConnectionHandler
```

### Remarks
Implementations are registered with a `ConnectionDispatcher`, which reads the
first bytes of each accepted connection, calls [CanHandle\(ReadOnlySequence&lt;byte&gt;\)](Proxyfan.Domain.Proxy.IConnectionHandler.md#Proxyfan.Domain.Proxy.IConnectionHandler.CanHandle(System.Buffers.ReadOnlySequence_byte_) 'Proxyfan\.Domain\.Proxy\.IConnectionHandler\.CanHandle\(System\.Buffers\.ReadOnlySequence\<byte\>\)') on each
registered handler in order, and delegates to the first handler that returns
[true](https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/builtin-types/bool 'https://docs\.microsoft\.com/en\-us/dotnet/csharp/language\-reference/builtin\-types/bool')\.
### Methods

<a name='Proxyfan.Domain.Proxy.IConnectionHandler.CanHandle(System.Buffers.ReadOnlySequence_byte_)'></a>

## IConnectionHandler\.CanHandle\(ReadOnlySequence\<byte\>\) Method

Determines whether this handler can process a connection whose opening bytes
match the protocol this handler is responsible for\.

```csharp
bool CanHandle(System.Buffers.ReadOnlySequence<byte> initialBytes);
```
#### Parameters

<a name='Proxyfan.Domain.Proxy.IConnectionHandler.CanHandle(System.Buffers.ReadOnlySequence_byte_).initialBytes'></a>

`initialBytes` [System\.Buffers\.ReadOnlySequence&lt;](https://learn.microsoft.com/en-us/dotnet/api/system.buffers.readonlysequence-1 'System\.Buffers\.ReadOnlySequence\`1')[System\.Byte](https://learn.microsoft.com/en-us/dotnet/api/system.byte 'System\.Byte')[&gt;](https://learn.microsoft.com/en-us/dotnet/api/system.buffers.readonlysequence-1 'System\.Buffers\.ReadOnlySequence\`1')

The first bytes received on the connection \(up to 8 bytes\)\.
May contain fewer bytes if the client closed the connection early\.

#### Returns
[System\.Boolean](https://learn.microsoft.com/en-us/dotnet/api/system.boolean 'System\.Boolean')  
[true](https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/builtin-types/bool 'https://docs\.microsoft\.com/en\-us/dotnet/csharp/language\-reference/builtin\-types/bool') if this handler recognises the protocol in
                [initialBytes](Proxyfan.Domain.Proxy.IConnectionHandler.md#Proxyfan.Domain.Proxy.IConnectionHandler.CanHandle(System.Buffers.ReadOnlySequence_byte_).initialBytes 'Proxyfan\.Domain\.Proxy\.IConnectionHandler\.CanHandle\(System\.Buffers\.ReadOnlySequence\<byte\>\)\.initialBytes'); [false](https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/builtin-types/bool 'https://docs\.microsoft\.com/en\-us/dotnet/csharp/language\-reference/builtin\-types/bool') if the bytes are
                insufficient or do not match this handler's protocol\.

<a name='Proxyfan.Domain.Proxy.IConnectionHandler.HandleAsync(Proxyfan.Domain.Proxy.IProxyConnection,System.Threading.CancellationToken)'></a>

## IConnectionHandler\.HandleAsync\(IProxyConnection, CancellationToken\) Method

Handles the connection\. The connection's transport pipe contains all original
bytes including those peeked during protocol detection, so implementations
may read from the start of the stream without special handling\.

```csharp
System.Threading.Tasks.Task HandleAsync(Proxyfan.Domain.Proxy.IProxyConnection connection, System.Threading.CancellationToken cancellationToken);
```
#### Parameters

<a name='Proxyfan.Domain.Proxy.IConnectionHandler.HandleAsync(Proxyfan.Domain.Proxy.IProxyConnection,System.Threading.CancellationToken).connection'></a>

`connection` [IProxyConnection](Proxyfan.Domain.Proxy.IProxyConnection.md 'Proxyfan\.Domain\.Proxy\.IProxyConnection')

The accepted connection to handle\.

<a name='Proxyfan.Domain.Proxy.IConnectionHandler.HandleAsync(Proxyfan.Domain.Proxy.IProxyConnection,System.Threading.CancellationToken).cancellationToken'></a>

`cancellationToken` [System\.Threading\.CancellationToken](https://learn.microsoft.com/en-us/dotnet/api/system.threading.cancellationtoken 'System\.Threading\.CancellationToken')

A token that, when cancelled, requests graceful termination\.

#### Returns
[System\.Threading\.Tasks\.Task](https://learn.microsoft.com/en-us/dotnet/api/system.threading.tasks.task 'System\.Threading\.Tasks\.Task')  
A [System\.Threading\.Tasks\.Task](https://learn.microsoft.com/en-us/dotnet/api/system.threading.tasks.task 'System\.Threading\.Tasks\.Task') that completes when the connection has been fully handled\.