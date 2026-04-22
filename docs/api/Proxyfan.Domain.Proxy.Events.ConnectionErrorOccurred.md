#### [Domain\.Proxy](index.md 'index')
### [Proxyfan\.Domain\.Proxy\.Events](Proxyfan.Domain.Proxy.Events.md 'Proxyfan\.Domain\.Proxy\.Events')

## ConnectionErrorOccurred Class

Published when a connection handler throws an unhandled exception while processing
an accepted connection\. The connection is closed after this event is published\.

```csharp
public sealed record ConnectionErrorOccurred : Proxyfan.Domain.IDomainEvent, System.IEquatable<Proxyfan.Domain.Proxy.Events.ConnectionErrorOccurred>
```

Inheritance [System\.Object](https://learn.microsoft.com/en-us/dotnet/api/system.object 'System\.Object') &#129106; ConnectionErrorOccurred

Implements [Proxyfan\.Domain\.IDomainEvent](https://learn.microsoft.com/en-us/dotnet/api/proxyfan.domain.idomainevent 'Proxyfan\.Domain\.IDomainEvent'), [System\.IEquatable&lt;](https://learn.microsoft.com/en-us/dotnet/api/system.iequatable-1 'System\.IEquatable\`1')[ConnectionErrorOccurred](Proxyfan.Domain.Proxy.Events.ConnectionErrorOccurred.md 'Proxyfan\.Domain\.Proxy\.Events\.ConnectionErrorOccurred')[&gt;](https://learn.microsoft.com/en-us/dotnet/api/system.iequatable-1 'System\.IEquatable\`1')
### Constructors

<a name='Proxyfan.Domain.Proxy.Events.ConnectionErrorOccurred.ConnectionErrorOccurred(System.Net.EndPoint,System.Exception,System.DateTimeOffset)'></a>

## ConnectionErrorOccurred\(EndPoint, Exception, DateTimeOffset\) Constructor

Published when a connection handler throws an unhandled exception while processing
an accepted connection\. The connection is closed after this event is published\.

```csharp
public ConnectionErrorOccurred(System.Net.EndPoint RemoteEndPoint, System.Exception Exception, System.DateTimeOffset Timestamp);
```
#### Parameters

<a name='Proxyfan.Domain.Proxy.Events.ConnectionErrorOccurred.ConnectionErrorOccurred(System.Net.EndPoint,System.Exception,System.DateTimeOffset).RemoteEndPoint'></a>

`RemoteEndPoint` [System\.Net\.EndPoint](https://learn.microsoft.com/en-us/dotnet/api/system.net.endpoint 'System\.Net\.EndPoint')

The remote endpoint of the client whose connection failed\.

<a name='Proxyfan.Domain.Proxy.Events.ConnectionErrorOccurred.ConnectionErrorOccurred(System.Net.EndPoint,System.Exception,System.DateTimeOffset).Exception'></a>

`Exception` [System\.Exception](https://learn.microsoft.com/en-us/dotnet/api/system.exception 'System\.Exception')

The exception that caused the connection to fail\.

<a name='Proxyfan.Domain.Proxy.Events.ConnectionErrorOccurred.ConnectionErrorOccurred(System.Net.EndPoint,System.Exception,System.DateTimeOffset).Timestamp'></a>

`Timestamp` [System\.DateTimeOffset](https://learn.microsoft.com/en-us/dotnet/api/system.datetimeoffset 'System\.DateTimeOffset')

The UTC instant at which the error occurred\.
### Properties

<a name='Proxyfan.Domain.Proxy.Events.ConnectionErrorOccurred.Exception'></a>

## ConnectionErrorOccurred\.Exception Property

The exception that caused the connection to fail\.

```csharp
public System.Exception Exception { get; init; }
```

#### Property Value
[System\.Exception](https://learn.microsoft.com/en-us/dotnet/api/system.exception 'System\.Exception')

<a name='Proxyfan.Domain.Proxy.Events.ConnectionErrorOccurred.RemoteEndPoint'></a>

## ConnectionErrorOccurred\.RemoteEndPoint Property

The remote endpoint of the client whose connection failed\.

```csharp
public System.Net.EndPoint RemoteEndPoint { get; init; }
```

#### Property Value
[System\.Net\.EndPoint](https://learn.microsoft.com/en-us/dotnet/api/system.net.endpoint 'System\.Net\.EndPoint')

<a name='Proxyfan.Domain.Proxy.Events.ConnectionErrorOccurred.Timestamp'></a>

## ConnectionErrorOccurred\.Timestamp Property

The UTC instant at which the error occurred\.

```csharp
public System.DateTimeOffset Timestamp { get; init; }
```

#### Property Value
[System\.DateTimeOffset](https://learn.microsoft.com/en-us/dotnet/api/system.datetimeoffset 'System\.DateTimeOffset')