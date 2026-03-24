#### [Domain\.Proxy](index.md 'index')
### [Proxyfan\.Domain\.Proxy](Proxyfan.Domain.Proxy.md 'Proxyfan\.Domain\.Proxy')

## ProxyAlreadyRunningError Class

Error raised when `StartAsync` is called while the proxy is already running or starting\.

```csharp
public sealed record ProxyAlreadyRunningError : Proxyfan.Domain.Proxy.ProxyError, System.IEquatable<Proxyfan.Domain.Proxy.ProxyAlreadyRunningError>
```

Inheritance [System\.Object](https://learn.microsoft.com/en-us/dotnet/api/system.object 'System\.Object') &#129106; [Proxyfan\.Domain\.DomainError](https://learn.microsoft.com/en-us/dotnet/api/proxyfan.domain.domainerror 'Proxyfan\.Domain\.DomainError') &#129106; [ProxyError](Proxyfan.Domain.Proxy.ProxyError.md 'Proxyfan\.Domain\.Proxy\.ProxyError') &#129106; ProxyAlreadyRunningError

Implements [System\.IEquatable&lt;](https://learn.microsoft.com/en-us/dotnet/api/system.iequatable-1 'System\.IEquatable\`1')[ProxyAlreadyRunningError](Proxyfan.Domain.Proxy.ProxyAlreadyRunningError.md 'Proxyfan\.Domain\.Proxy\.ProxyAlreadyRunningError')[&gt;](https://learn.microsoft.com/en-us/dotnet/api/system.iequatable-1 'System\.IEquatable\`1')
### Constructors

<a name='Proxyfan.Domain.Proxy.ProxyAlreadyRunningError.ProxyAlreadyRunningError()'></a>

## ProxyAlreadyRunningError\(\) Constructor

Error raised when `StartAsync` is called while the proxy is already running or starting\.

```csharp
public ProxyAlreadyRunningError();
```