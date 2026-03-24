#### [Domain\.Proxy](index.md 'index')
### [Proxyfan\.Domain\.Proxy](Proxyfan.Domain.Proxy.md 'Proxyfan\.Domain\.Proxy')

## ProxyBindError Class

Error raised when the proxy listener fails to bind to the configured port\.

```csharp
public sealed record ProxyBindError : Proxyfan.Domain.Proxy.ProxyError, System.IEquatable<Proxyfan.Domain.Proxy.ProxyBindError>
```

Inheritance [System\.Object](https://learn.microsoft.com/en-us/dotnet/api/system.object 'System\.Object') &#129106; [Proxyfan\.Domain\.DomainError](https://learn.microsoft.com/en-us/dotnet/api/proxyfan.domain.domainerror 'Proxyfan\.Domain\.DomainError') &#129106; [ProxyError](Proxyfan.Domain.Proxy.ProxyError.md 'Proxyfan\.Domain\.Proxy\.ProxyError') &#129106; ProxyBindError

Implements [System\.IEquatable&lt;](https://learn.microsoft.com/en-us/dotnet/api/system.iequatable-1 'System\.IEquatable\`1')[ProxyBindError](Proxyfan.Domain.Proxy.ProxyBindError.md 'Proxyfan\.Domain\.Proxy\.ProxyBindError')[&gt;](https://learn.microsoft.com/en-us/dotnet/api/system.iequatable-1 'System\.IEquatable\`1')
### Constructors

<a name='Proxyfan.Domain.Proxy.ProxyBindError.ProxyBindError(int,System.Exception)'></a>

## ProxyBindError\(int, Exception\) Constructor

Error raised when the proxy listener fails to bind to the configured port\.

```csharp
public ProxyBindError(int Port, System.Exception InnerException);
```
#### Parameters

<a name='Proxyfan.Domain.Proxy.ProxyBindError.ProxyBindError(int,System.Exception).Port'></a>

`Port` [System\.Int32](https://learn.microsoft.com/en-us/dotnet/api/system.int32 'System\.Int32')

The port number that could not be bound\.

<a name='Proxyfan.Domain.Proxy.ProxyBindError.ProxyBindError(int,System.Exception).InnerException'></a>

`InnerException` [System\.Exception](https://learn.microsoft.com/en-us/dotnet/api/system.exception 'System\.Exception')

The underlying bind exception\.
### Properties

<a name='Proxyfan.Domain.Proxy.ProxyBindError.Port'></a>

## ProxyBindError\.Port Property

The port number that could not be bound\.

```csharp
public int Port { get; init; }
```

#### Property Value
[System\.Int32](https://learn.microsoft.com/en-us/dotnet/api/system.int32 'System\.Int32')