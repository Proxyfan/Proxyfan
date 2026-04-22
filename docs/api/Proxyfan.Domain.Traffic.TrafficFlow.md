### [Proxyfan\.Domain\.Traffic](Proxyfan.Domain.Traffic.md 'Proxyfan\.Domain\.Traffic')

## TrafficFlow Class

Represents a single proxy traffic flow — one client connection from acceptance
to completion or failure\.

```csharp
public sealed class TrafficFlow
```

Inheritance [System\.Object](https://learn.microsoft.com/en-us/dotnet/api/system.object 'System\.Object') &#129106; TrafficFlow
### Constructors

<a name='Proxyfan.Domain.Traffic.TrafficFlow.TrafficFlow(System.Guid,string,System.DateTimeOffset)'></a>

## TrafficFlow\(Guid, string, DateTimeOffset\) Constructor

Represents a single proxy traffic flow — one client connection from acceptance
to completion or failure\.

```csharp
public TrafficFlow(System.Guid id, string clientEndPoint, System.DateTimeOffset startedAt);
```
#### Parameters

<a name='Proxyfan.Domain.Traffic.TrafficFlow.TrafficFlow(System.Guid,string,System.DateTimeOffset).id'></a>

`id` [System\.Guid](https://learn.microsoft.com/en-us/dotnet/api/system.guid 'System\.Guid')

A unique identifier for this flow\.

<a name='Proxyfan.Domain.Traffic.TrafficFlow.TrafficFlow(System.Guid,string,System.DateTimeOffset).clientEndPoint'></a>

`clientEndPoint` [System\.String](https://learn.microsoft.com/en-us/dotnet/api/system.string 'System\.String')

The string representation of the client's remote endpoint\.

<a name='Proxyfan.Domain.Traffic.TrafficFlow.TrafficFlow(System.Guid,string,System.DateTimeOffset).startedAt'></a>

`startedAt` [System\.DateTimeOffset](https://learn.microsoft.com/en-us/dotnet/api/system.datetimeoffset 'System\.DateTimeOffset')

The UTC instant at which the connection was accepted\.
### Properties

<a name='Proxyfan.Domain.Traffic.TrafficFlow.ClientEndPoint'></a>

## TrafficFlow\.ClientEndPoint Property

Gets the string representation of the client's remote endpoint \(e\.g\., `"127.0.0.1:54321"`\)\.

```csharp
public string ClientEndPoint { get; }
```

#### Property Value
[System\.String](https://learn.microsoft.com/en-us/dotnet/api/system.string 'System\.String')

<a name='Proxyfan.Domain.Traffic.TrafficFlow.FailedAt'></a>

## TrafficFlow\.FailedAt Property

Gets the UTC instant at which the flow transitioned to [Failed](Proxyfan.Domain.Traffic.TrafficFlowStatus.md#Proxyfan.Domain.Traffic.TrafficFlowStatus.Failed 'Proxyfan\.Domain\.Traffic\.TrafficFlowStatus\.Failed'),
or [null](https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/null 'https://docs\.microsoft\.com/en\-us/dotnet/csharp/language\-reference/keywords/null') if the flow has not failed\.

```csharp
public System.Nullable<System.DateTimeOffset> FailedAt { get; }
```

#### Property Value
[System\.Nullable&lt;](https://learn.microsoft.com/en-us/dotnet/api/system.nullable-1 'System\.Nullable\`1')[System\.DateTimeOffset](https://learn.microsoft.com/en-us/dotnet/api/system.datetimeoffset 'System\.DateTimeOffset')[&gt;](https://learn.microsoft.com/en-us/dotnet/api/system.nullable-1 'System\.Nullable\`1')

<a name='Proxyfan.Domain.Traffic.TrafficFlow.Id'></a>

## TrafficFlow\.Id Property

Gets the unique identifier of this flow\.

```csharp
public System.Guid Id { get; }
```

#### Property Value
[System\.Guid](https://learn.microsoft.com/en-us/dotnet/api/system.guid 'System\.Guid')

<a name='Proxyfan.Domain.Traffic.TrafficFlow.StartedAt'></a>

## TrafficFlow\.StartedAt Property

Gets the UTC instant at which the connection was accepted\.

```csharp
public System.DateTimeOffset StartedAt { get; }
```

#### Property Value
[System\.DateTimeOffset](https://learn.microsoft.com/en-us/dotnet/api/system.datetimeoffset 'System\.DateTimeOffset')

<a name='Proxyfan.Domain.Traffic.TrafficFlow.Status'></a>

## TrafficFlow\.Status Property

Gets the current lifecycle status of this flow\.

```csharp
public Proxyfan.Domain.Traffic.TrafficFlowStatus Status { get; }
```

#### Property Value
[TrafficFlowStatus](Proxyfan.Domain.Traffic.TrafficFlowStatus.md 'Proxyfan\.Domain\.Traffic\.TrafficFlowStatus')
### Methods

<a name='Proxyfan.Domain.Traffic.TrafficFlow.Fail()'></a>

## TrafficFlow\.Fail\(\) Method

Transitions this flow to [Failed](Proxyfan.Domain.Traffic.TrafficFlowStatus.md#Proxyfan.Domain.Traffic.TrafficFlowStatus.Failed 'Proxyfan\.Domain\.Traffic\.TrafficFlowStatus\.Failed') and records the failure time\.
If the flow is already [Failed](Proxyfan.Domain.Traffic.TrafficFlowStatus.md#Proxyfan.Domain.Traffic.TrafficFlowStatus.Failed 'Proxyfan\.Domain\.Traffic\.TrafficFlowStatus\.Failed'), this method is a no\-op\.

```csharp
public void Fail();
```