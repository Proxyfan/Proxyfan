### [Proxyfan\.Domain](Proxyfan.Domain.md 'Proxyfan\.Domain')

## DomainEventBus Class

In\-process domain event bus backed by a [System\.Collections\.Concurrent\.ConcurrentDictionary&lt;&gt;](https://learn.microsoft.com/en-us/dotnet/api/system.collections.concurrent.concurrentdictionary-2 'System\.Collections\.Concurrent\.ConcurrentDictionary\`2')
of handler lists\. Delivery is synchronous on the calling thread\.

```csharp
public sealed class DomainEventBus : Proxyfan.Domain.IDomainEventBus
```

Inheritance [System\.Object](https://learn.microsoft.com/en-us/dotnet/api/system.object 'System\.Object') &#129106; DomainEventBus

Implements [IDomainEventBus](Proxyfan.Domain.IDomainEventBus.md 'Proxyfan\.Domain\.IDomainEventBus')
### Methods

<a name='Proxyfan.Domain.DomainEventBus.Publish_TEvent_(TEvent)'></a>

## DomainEventBus\.Publish\<TEvent\>\(TEvent\) Method

Publishes a domain event, synchronously invoking all registered handlers for
[TEvent](Proxyfan.Domain.DomainEventBus.md#Proxyfan.Domain.DomainEventBus.Publish_TEvent_(TEvent).TEvent 'Proxyfan\.Domain\.DomainEventBus\.Publish\<TEvent\>\(TEvent\)\.TEvent')\.

```csharp
public void Publish<TEvent>(TEvent domainEvent)
    where TEvent : Proxyfan.Domain.IDomainEvent;
```
#### Type parameters

<a name='Proxyfan.Domain.DomainEventBus.Publish_TEvent_(TEvent).TEvent'></a>

`TEvent`

The domain event type to publish\.
#### Parameters

<a name='Proxyfan.Domain.DomainEventBus.Publish_TEvent_(TEvent).domainEvent'></a>

`domainEvent` [TEvent](Proxyfan.Domain.DomainEventBus.md#Proxyfan.Domain.DomainEventBus.Publish_TEvent_(TEvent).TEvent 'Proxyfan\.Domain\.DomainEventBus\.Publish\<TEvent\>\(TEvent\)\.TEvent')

The event instance to deliver to subscribers\.

Implements [Publish&lt;TEvent&gt;\(TEvent\)](Proxyfan.Domain.IDomainEventBus.md#Proxyfan.Domain.IDomainEventBus.Publish_TEvent_(TEvent) 'Proxyfan\.Domain\.IDomainEventBus\.Publish\<TEvent\>\(TEvent\)')

<a name='Proxyfan.Domain.DomainEventBus.Subscribe_TEvent_(System.Action_TEvent_)'></a>

## DomainEventBus\.Subscribe\<TEvent\>\(Action\<TEvent\>\) Method

Registers a handler to be invoked whenever an event of type
[TEvent](Proxyfan.Domain.DomainEventBus.md#Proxyfan.Domain.DomainEventBus.Subscribe_TEvent_(System.Action_TEvent_).TEvent 'Proxyfan\.Domain\.DomainEventBus\.Subscribe\<TEvent\>\(System\.Action\<TEvent\>\)\.TEvent') is published\.

```csharp
public System.IDisposable Subscribe<TEvent>(System.Action<TEvent> handler)
    where TEvent : Proxyfan.Domain.IDomainEvent;
```
#### Type parameters

<a name='Proxyfan.Domain.DomainEventBus.Subscribe_TEvent_(System.Action_TEvent_).TEvent'></a>

`TEvent`

The domain event type to subscribe to\.
#### Parameters

<a name='Proxyfan.Domain.DomainEventBus.Subscribe_TEvent_(System.Action_TEvent_).handler'></a>

`handler` [System\.Action&lt;](https://learn.microsoft.com/en-us/dotnet/api/system.action-1 'System\.Action\`1')[TEvent](Proxyfan.Domain.DomainEventBus.md#Proxyfan.Domain.DomainEventBus.Subscribe_TEvent_(System.Action_TEvent_).TEvent 'Proxyfan\.Domain\.DomainEventBus\.Subscribe\<TEvent\>\(System\.Action\<TEvent\>\)\.TEvent')[&gt;](https://learn.microsoft.com/en-us/dotnet/api/system.action-1 'System\.Action\`1')

The handler invoked when the event is published\.

Implements [Subscribe&lt;TEvent&gt;\(Action&lt;TEvent&gt;\)](Proxyfan.Domain.IDomainEventBus.md#Proxyfan.Domain.IDomainEventBus.Subscribe_TEvent_(System.Action_TEvent_) 'Proxyfan\.Domain\.IDomainEventBus\.Subscribe\<TEvent\>\(System\.Action\<TEvent\>\)')

#### Returns
[System\.IDisposable](https://learn.microsoft.com/en-us/dotnet/api/system.idisposable 'System\.IDisposable')  
An [System\.IDisposable](https://learn.microsoft.com/en-us/dotnet/api/system.idisposable 'System\.IDisposable') that, when disposed, removes the handler registration\.