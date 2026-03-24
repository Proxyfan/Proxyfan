#### [Domain\.Proxy](index.md 'index')
### [Proxyfan\.Domain\.Proxy\.Events](Proxyfan.Domain.Proxy.Events.md 'Proxyfan\.Domain\.Proxy\.Events')

## ProxyStarted Class

Published when the proxy server successfully starts listening on a port\.

```csharp
public sealed record ProxyStarted : Proxyfan.Domain.IDomainEvent, System.IEquatable<Proxyfan.Domain.Proxy.Events.ProxyStarted>
```

Inheritance [System\.Object](https://learn.microsoft.com/en-us/dotnet/api/system.object 'System\.Object') &#129106; ProxyStarted

Implements [Proxyfan\.Domain\.IDomainEvent](https://learn.microsoft.com/en-us/dotnet/api/proxyfan.domain.idomainevent 'Proxyfan\.Domain\.IDomainEvent'), [System\.IEquatable&lt;](https://learn.microsoft.com/en-us/dotnet/api/system.iequatable-1 'System\.IEquatable\`1')[ProxyStarted](Proxyfan.Domain.Proxy.Events.ProxyStarted.md 'Proxyfan\.Domain\.Proxy\.Events\.ProxyStarted')[&gt;](https://learn.microsoft.com/en-us/dotnet/api/system.iequatable-1 'System\.IEquatable\`1')
### Constructors

<a name='Proxyfan.Domain.Proxy.Events.ProxyStarted.ProxyStarted(int,System.DateTimeOffset)'></a>

## ProxyStarted\(int, DateTimeOffset\) Constructor

Published when the proxy server successfully starts listening on a port\.

```csharp
public ProxyStarted(int Port, System.DateTimeOffset Timestamp);
```
#### Parameters

<a name='Proxyfan.Domain.Proxy.Events.ProxyStarted.ProxyStarted(int,System.DateTimeOffset).Port'></a>

`Port` [System\.Int32](https://learn.microsoft.com/en-us/dotnet/api/system.int32 'System\.Int32')

The port the proxy is now listening on\.

<a name='Proxyfan.Domain.Proxy.Events.ProxyStarted.ProxyStarted(int,System.DateTimeOffset).Timestamp'></a>

`Timestamp` [System\.DateTimeOffset](https://learn.microsoft.com/en-us/dotnet/api/system.datetimeoffset 'System\.DateTimeOffset')

The UTC instant at which the proxy started\.
### Properties

<a name='Proxyfan.Domain.Proxy.Events.ProxyStarted.Port'></a>

## ProxyStarted\.Port Property

The port the proxy is now listening on\.

```csharp
public int Port { get; init; }
```

#### Property Value
[System\.Int32](https://learn.microsoft.com/en-us/dotnet/api/system.int32 'System\.Int32')

<a name='Proxyfan.Domain.Proxy.Events.ProxyStarted.Timestamp'></a>

## ProxyStarted\.Timestamp Property

The UTC instant at which the proxy started\.

```csharp
public System.DateTimeOffset Timestamp { get; init; }
```

#### Property Value
[System\.DateTimeOffset](https://learn.microsoft.com/en-us/dotnet/api/system.datetimeoffset 'System\.DateTimeOffset')