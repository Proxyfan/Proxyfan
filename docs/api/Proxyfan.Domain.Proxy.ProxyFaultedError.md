#### [Domain\.Proxy](index.md 'index')
### [Proxyfan\.Domain\.Proxy](Proxyfan.Domain.Proxy.md 'Proxyfan\.Domain\.Proxy')

## ProxyFaultedError Class

Error raised when a lifecycle operation fails due to an unexpected exception\.

```csharp
public sealed record ProxyFaultedError : Proxyfan.Domain.Proxy.ProxyError, System.IEquatable<Proxyfan.Domain.Proxy.ProxyFaultedError>
```

Inheritance [System\.Object](https://learn.microsoft.com/en-us/dotnet/api/system.object 'System\.Object') &#129106; [Proxyfan\.Domain\.DomainError](https://learn.microsoft.com/en-us/dotnet/api/proxyfan.domain.domainerror 'Proxyfan\.Domain\.DomainError') &#129106; [ProxyError](Proxyfan.Domain.Proxy.ProxyError.md 'Proxyfan\.Domain\.Proxy\.ProxyError') &#129106; ProxyFaultedError

Implements [System\.IEquatable&lt;](https://learn.microsoft.com/en-us/dotnet/api/system.iequatable-1 'System\.IEquatable\`1')[ProxyFaultedError](Proxyfan.Domain.Proxy.ProxyFaultedError.md 'Proxyfan\.Domain\.Proxy\.ProxyFaultedError')[&gt;](https://learn.microsoft.com/en-us/dotnet/api/system.iequatable-1 'System\.IEquatable\`1')
### Constructors

<a name='Proxyfan.Domain.Proxy.ProxyFaultedError.ProxyFaultedError(string,System.Exception)'></a>

## ProxyFaultedError\(string, Exception\) Constructor

Error raised when a lifecycle operation fails due to an unexpected exception\.

```csharp
public ProxyFaultedError(string Operation, System.Exception InnerException);
```
#### Parameters

<a name='Proxyfan.Domain.Proxy.ProxyFaultedError.ProxyFaultedError(string,System.Exception).Operation'></a>

`Operation` [System\.String](https://learn.microsoft.com/en-us/dotnet/api/system.string 'System\.String')

The operation that failed \(e\.g\., `"Start"`, `"Stop"`\)\.

<a name='Proxyfan.Domain.Proxy.ProxyFaultedError.ProxyFaultedError(string,System.Exception).InnerException'></a>

`InnerException` [System\.Exception](https://learn.microsoft.com/en-us/dotnet/api/system.exception 'System\.Exception')

The exception that caused the failure\.
### Properties

<a name='Proxyfan.Domain.Proxy.ProxyFaultedError.Operation'></a>

## ProxyFaultedError\.Operation Property

The operation that failed \(e\.g\., `"Start"`, `"Stop"`\)\.

```csharp
public string Operation { get; init; }
```

#### Property Value
[System\.String](https://learn.microsoft.com/en-us/dotnet/api/system.string 'System\.String')