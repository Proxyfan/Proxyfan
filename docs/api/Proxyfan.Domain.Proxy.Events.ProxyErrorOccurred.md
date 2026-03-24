#### [Domain\.Proxy](index.md 'index')
### [Proxyfan\.Domain\.Proxy\.Events](Proxyfan.Domain.Proxy.Events.md 'Proxyfan\.Domain\.Proxy\.Events')

## ProxyErrorOccurred Class

Published when the proxy server encounters an error during a lifecycle operation
\(start, stop, or restart\)\.

```csharp
public sealed record ProxyErrorOccurred : Proxyfan.Domain.IDomainEvent, System.IEquatable<Proxyfan.Domain.Proxy.Events.ProxyErrorOccurred>
```

Inheritance [System\.Object](https://learn.microsoft.com/en-us/dotnet/api/system.object 'System\.Object') &#129106; ProxyErrorOccurred

Implements [Proxyfan\.Domain\.IDomainEvent](https://learn.microsoft.com/en-us/dotnet/api/proxyfan.domain.idomainevent 'Proxyfan\.Domain\.IDomainEvent'), [System\.IEquatable&lt;](https://learn.microsoft.com/en-us/dotnet/api/system.iequatable-1 'System\.IEquatable\`1')[ProxyErrorOccurred](Proxyfan.Domain.Proxy.Events.ProxyErrorOccurred.md 'Proxyfan\.Domain\.Proxy\.Events\.ProxyErrorOccurred')[&gt;](https://learn.microsoft.com/en-us/dotnet/api/system.iequatable-1 'System\.IEquatable\`1')
### Constructors

<a name='Proxyfan.Domain.Proxy.Events.ProxyErrorOccurred.ProxyErrorOccurred(Proxyfan.Domain.Proxy.ProxyError,System.DateTimeOffset)'></a>

## ProxyErrorOccurred\(ProxyError, DateTimeOffset\) Constructor

Published when the proxy server encounters an error during a lifecycle operation
\(start, stop, or restart\)\.

```csharp
public ProxyErrorOccurred(Proxyfan.Domain.Proxy.ProxyError Error, System.DateTimeOffset Timestamp);
```
#### Parameters

<a name='Proxyfan.Domain.Proxy.Events.ProxyErrorOccurred.ProxyErrorOccurred(Proxyfan.Domain.Proxy.ProxyError,System.DateTimeOffset).Error'></a>

`Error` [ProxyError](Proxyfan.Domain.Proxy.ProxyError.md 'Proxyfan\.Domain\.Proxy\.ProxyError')

The domain error describing the failure\.

<a name='Proxyfan.Domain.Proxy.Events.ProxyErrorOccurred.ProxyErrorOccurred(Proxyfan.Domain.Proxy.ProxyError,System.DateTimeOffset).Timestamp'></a>

`Timestamp` [System\.DateTimeOffset](https://learn.microsoft.com/en-us/dotnet/api/system.datetimeoffset 'System\.DateTimeOffset')

The UTC instant at which the error occurred\.
### Properties

<a name='Proxyfan.Domain.Proxy.Events.ProxyErrorOccurred.Error'></a>

## ProxyErrorOccurred\.Error Property

The domain error describing the failure\.

```csharp
public Proxyfan.Domain.Proxy.ProxyError Error { get; init; }
```

#### Property Value
[ProxyError](Proxyfan.Domain.Proxy.ProxyError.md 'Proxyfan\.Domain\.Proxy\.ProxyError')

<a name='Proxyfan.Domain.Proxy.Events.ProxyErrorOccurred.Timestamp'></a>

## ProxyErrorOccurred\.Timestamp Property

The UTC instant at which the error occurred\.

```csharp
public System.DateTimeOffset Timestamp { get; init; }
```

#### Property Value
[System\.DateTimeOffset](https://learn.microsoft.com/en-us/dotnet/api/system.datetimeoffset 'System\.DateTimeOffset')