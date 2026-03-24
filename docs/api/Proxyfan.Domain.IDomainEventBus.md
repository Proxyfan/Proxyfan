### [Proxyfan\.Domain](Proxyfan.Domain.md 'Proxyfan\.Domain')

## IDomainEventBus Interface

Defines an in\-process domain event bus for publishing and subscribing to domain events
across bounded contexts\.

```csharp
public interface IDomainEventBus
```

Derived  
&#8627; [DomainEventBus](Proxyfan.Domain.DomainEventBus.md 'Proxyfan\.Domain\.DomainEventBus')

### Remarks
Delivery is synchronous and fire\-and\-forget: [Publish&lt;TEvent&gt;\(TEvent\)](Proxyfan.Domain.IDomainEventBus.md#Proxyfan.Domain.IDomainEventBus.Publish_TEvent_(TEvent) 'Proxyfan\.Domain\.IDomainEventBus\.Publish\<TEvent\>\(TEvent\)') calls all
registered handlers inline on the calling thread\. Handler exceptions are caught and do
not prevent subsequent handlers from executing\.
### Methods

<a name='Proxyfan.Domain.IDomainEventBus.Publish_TEvent_(TEvent)'></a>

## IDomainEventBus\.Publish\<TEvent\>\(TEvent\) Method

Publishes a domain event, synchronously invoking all registered handlers for
[TEvent](Proxyfan.Domain.IDomainEventBus.md#Proxyfan.Domain.IDomainEventBus.Publish_TEvent_(TEvent).TEvent 'Proxyfan\.Domain\.IDomainEventBus\.Publish\<TEvent\>\(TEvent\)\.TEvent')\.

```csharp
void Publish<TEvent>(TEvent domainEvent)
    where TEvent : Proxyfan.Domain.IDomainEvent;
```
#### Type parameters

<a name='Proxyfan.Domain.IDomainEventBus.Publish_TEvent_(TEvent).TEvent'></a>

`TEvent`

The domain event type to publish\.
#### Parameters

<a name='Proxyfan.Domain.IDomainEventBus.Publish_TEvent_(TEvent).domainEvent'></a>

`domainEvent` [TEvent](Proxyfan.Domain.IDomainEventBus.md#Proxyfan.Domain.IDomainEventBus.Publish_TEvent_(TEvent).TEvent 'Proxyfan\.Domain\.IDomainEventBus\.Publish\<TEvent\>\(TEvent\)\.TEvent')

The event instance to deliver to subscribers\.

<a name='Proxyfan.Domain.IDomainEventBus.Subscribe_TEvent_(System.Action_TEvent_)'></a>

## IDomainEventBus\.Subscribe\<TEvent\>\(Action\<TEvent\>\) Method

Registers a handler to be invoked whenever an event of type
[TEvent](Proxyfan.Domain.IDomainEventBus.md#Proxyfan.Domain.IDomainEventBus.Subscribe_TEvent_(System.Action_TEvent_).TEvent 'Proxyfan\.Domain\.IDomainEventBus\.Subscribe\<TEvent\>\(System\.Action\<TEvent\>\)\.TEvent') is published\.

```csharp
System.IDisposable Subscribe<TEvent>(System.Action<TEvent> handler)
    where TEvent : Proxyfan.Domain.IDomainEvent;
```
#### Type parameters

<a name='Proxyfan.Domain.IDomainEventBus.Subscribe_TEvent_(System.Action_TEvent_).TEvent'></a>

`TEvent`

The domain event type to subscribe to\.
#### Parameters

<a name='Proxyfan.Domain.IDomainEventBus.Subscribe_TEvent_(System.Action_TEvent_).handler'></a>

`handler` [System\.Action&lt;](https://learn.microsoft.com/en-us/dotnet/api/system.action-1 'System\.Action\`1')[TEvent](Proxyfan.Domain.IDomainEventBus.md#Proxyfan.Domain.IDomainEventBus.Subscribe_TEvent_(System.Action_TEvent_).TEvent 'Proxyfan\.Domain\.IDomainEventBus\.Subscribe\<TEvent\>\(System\.Action\<TEvent\>\)\.TEvent')[&gt;](https://learn.microsoft.com/en-us/dotnet/api/system.action-1 'System\.Action\`1')

The handler invoked when the event is published\.

#### Returns
[System\.IDisposable](https://learn.microsoft.com/en-us/dotnet/api/system.idisposable 'System\.IDisposable')  
An [System\.IDisposable](https://learn.microsoft.com/en-us/dotnet/api/system.idisposable 'System\.IDisposable') that, when disposed, removes the handler registration\.