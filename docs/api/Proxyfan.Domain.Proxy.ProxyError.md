#### [Domain\.Proxy](index.md 'index')
### [Proxyfan\.Domain\.Proxy](Proxyfan.Domain.Proxy.md 'Proxyfan\.Domain\.Proxy')

## ProxyError Class

Base record for all proxy\-specific domain errors\.

```csharp
public abstract record ProxyError : Proxyfan.Domain.DomainError, System.IEquatable<Proxyfan.Domain.Proxy.ProxyError>
```

Inheritance [System\.Object](https://learn.microsoft.com/en-us/dotnet/api/system.object 'System\.Object') &#129106; [Proxyfan\.Domain\.DomainError](https://learn.microsoft.com/en-us/dotnet/api/proxyfan.domain.domainerror 'Proxyfan\.Domain\.DomainError') &#129106; ProxyError

Derived  
&#8627; [ProxyAlreadyRunningError](Proxyfan.Domain.Proxy.ProxyAlreadyRunningError.md 'Proxyfan\.Domain\.Proxy\.ProxyAlreadyRunningError')  
&#8627; [ProxyBindError](Proxyfan.Domain.Proxy.ProxyBindError.md 'Proxyfan\.Domain\.Proxy\.ProxyBindError')  
&#8627; [ProxyFaultedError](Proxyfan.Domain.Proxy.ProxyFaultedError.md 'Proxyfan\.Domain\.Proxy\.ProxyFaultedError')  
&#8627; [ProxyNotRunningError](Proxyfan.Domain.Proxy.ProxyNotRunningError.md 'Proxyfan\.Domain\.Proxy\.ProxyNotRunningError')

Implements [System\.IEquatable&lt;](https://learn.microsoft.com/en-us/dotnet/api/system.iequatable-1 'System\.IEquatable\`1')[ProxyError](Proxyfan.Domain.Proxy.ProxyError.md 'Proxyfan\.Domain\.Proxy\.ProxyError')[&gt;](https://learn.microsoft.com/en-us/dotnet/api/system.iequatable-1 'System\.IEquatable\`1')
### Constructors

<a name='Proxyfan.Domain.Proxy.ProxyError.ProxyError(string,string,System.Exception)'></a>

## ProxyError\(string, string, Exception\) Constructor

Base record for all proxy\-specific domain errors\.

```csharp
protected ProxyError(string Code, string Message, System.Exception? InnerException=null);
```
#### Parameters

<a name='Proxyfan.Domain.Proxy.ProxyError.ProxyError(string,string,System.Exception).Code'></a>

`Code` [System\.String](https://learn.microsoft.com/en-us/dotnet/api/system.string 'System\.String')

Machine\-readable error code\.

<a name='Proxyfan.Domain.Proxy.ProxyError.ProxyError(string,string,System.Exception).Message'></a>

`Message` [System\.String](https://learn.microsoft.com/en-us/dotnet/api/system.string 'System\.String')

Human\-readable error description\.

<a name='Proxyfan.Domain.Proxy.ProxyError.ProxyError(string,string,System.Exception).InnerException'></a>

`InnerException` [System\.Exception](https://learn.microsoft.com/en-us/dotnet/api/system.exception 'System\.Exception')

Optional underlying exception\.