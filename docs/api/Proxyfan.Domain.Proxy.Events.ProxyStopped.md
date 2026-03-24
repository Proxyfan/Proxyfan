#### [Domain\.Proxy](index.md 'index')
### [Proxyfan\.Domain\.Proxy\.Events](Proxyfan.Domain.Proxy.Events.md 'Proxyfan\.Domain\.Proxy\.Events')

## ProxyStopped Class

Published when the proxy server stops listening, either via an explicit stop or dispose\.

```csharp
public sealed record ProxyStopped : Proxyfan.Domain.IDomainEvent, System.IEquatable<Proxyfan.Domain.Proxy.Events.ProxyStopped>
```

Inheritance [System\.Object](https://learn.microsoft.com/en-us/dotnet/api/system.object 'System\.Object') &#129106; ProxyStopped

Implements [Proxyfan\.Domain\.IDomainEvent](https://learn.microsoft.com/en-us/dotnet/api/proxyfan.domain.idomainevent 'Proxyfan\.Domain\.IDomainEvent'), [System\.IEquatable&lt;](https://learn.microsoft.com/en-us/dotnet/api/system.iequatable-1 'System\.IEquatable\`1')[ProxyStopped](Proxyfan.Domain.Proxy.Events.ProxyStopped.md 'Proxyfan\.Domain\.Proxy\.Events\.ProxyStopped')[&gt;](https://learn.microsoft.com/en-us/dotnet/api/system.iequatable-1 'System\.IEquatable\`1')
### Constructors

<a name='Proxyfan.Domain.Proxy.Events.ProxyStopped.ProxyStopped(System.DateTimeOffset)'></a>

## ProxyStopped\(DateTimeOffset\) Constructor

Published when the proxy server stops listening, either via an explicit stop or dispose\.

```csharp
public ProxyStopped(System.DateTimeOffset Timestamp);
```
#### Parameters

<a name='Proxyfan.Domain.Proxy.Events.ProxyStopped.ProxyStopped(System.DateTimeOffset).Timestamp'></a>

`Timestamp` [System\.DateTimeOffset](https://learn.microsoft.com/en-us/dotnet/api/system.datetimeoffset 'System\.DateTimeOffset')

The UTC instant at which the proxy stopped\.
### Properties

<a name='Proxyfan.Domain.Proxy.Events.ProxyStopped.Timestamp'></a>

## ProxyStopped\.Timestamp Property

The UTC instant at which the proxy stopped\.

```csharp
public System.DateTimeOffset Timestamp { get; init; }
```

#### Property Value
[System\.DateTimeOffset](https://learn.microsoft.com/en-us/dotnet/api/system.datetimeoffset 'System\.DateTimeOffset')