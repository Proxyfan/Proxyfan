#### [Domain\.Proxy](index.md 'index')
### [Proxyfan\.Domain\.Proxy](Proxyfan.Domain.Proxy.md 'Proxyfan\.Domain\.Proxy')

## ProxyNotRunningError Class

Error raised when `StopAsync` is called while the proxy is already stopped or stopping\.

```csharp
public sealed record ProxyNotRunningError : Proxyfan.Domain.Proxy.ProxyError, System.IEquatable<Proxyfan.Domain.Proxy.ProxyNotRunningError>
```

Inheritance [System\.Object](https://learn.microsoft.com/en-us/dotnet/api/system.object 'System\.Object') &#129106; [Proxyfan\.Domain\.DomainError](https://learn.microsoft.com/en-us/dotnet/api/proxyfan.domain.domainerror 'Proxyfan\.Domain\.DomainError') &#129106; [ProxyError](Proxyfan.Domain.Proxy.ProxyError.md 'Proxyfan\.Domain\.Proxy\.ProxyError') &#129106; ProxyNotRunningError

Implements [System\.IEquatable&lt;](https://learn.microsoft.com/en-us/dotnet/api/system.iequatable-1 'System\.IEquatable\`1')[ProxyNotRunningError](Proxyfan.Domain.Proxy.ProxyNotRunningError.md 'Proxyfan\.Domain\.Proxy\.ProxyNotRunningError')[&gt;](https://learn.microsoft.com/en-us/dotnet/api/system.iequatable-1 'System\.IEquatable\`1')
### Constructors

<a name='Proxyfan.Domain.Proxy.ProxyNotRunningError.ProxyNotRunningError()'></a>

## ProxyNotRunningError\(\) Constructor

Error raised when `StopAsync` is called while the proxy is already stopped or stopping\.

```csharp
public ProxyNotRunningError();
```