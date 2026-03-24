## Proxyfan\.Domain Namespace

| Classes | |
| :--- | :--- |
| [DomainError](Proxyfan.Domain.DomainError.md 'Proxyfan\.Domain\.DomainError') | Base record for all domain errors, carrying a machine\-readable code and a human\-readable message\. |
| [DomainEventBus](Proxyfan.Domain.DomainEventBus.md 'Proxyfan\.Domain\.DomainEventBus') | In\-process domain event bus backed by a [System\.Collections\.Concurrent\.ConcurrentDictionary&lt;&gt;](https://learn.microsoft.com/en-us/dotnet/api/system.collections.concurrent.concurrentdictionary-2 'System\.Collections\.Concurrent\.ConcurrentDictionary\`2') of handler lists\. Delivery is synchronous on the calling thread\. |
| [Result](Proxyfan.Domain.Result.md 'Proxyfan\.Domain\.Result') | Represents the outcome of a domain operation that produces no value\. Also provides factory methods for creating [Result&lt;T&gt;](Proxyfan.Domain.Result_T_.md 'Proxyfan\.Domain\.Result\<T\>') instances\. |
| [Result&lt;T&gt;](Proxyfan.Domain.Result_T_.md 'Proxyfan\.Domain\.Result\<T\>') | Represents the outcome of a domain operation that produces a value of type [T](Proxyfan.Domain.Result_T_.md#Proxyfan.Domain.Result_T_.T 'Proxyfan\.Domain\.Result\<T\>\.T')\. |

| Interfaces | |
| :--- | :--- |
| [IDomainEvent](Proxyfan.Domain.IDomainEvent.md 'Proxyfan\.Domain\.IDomainEvent') | Marker interface for all domain events published through the domain event bus\. |
| [IDomainEventBus](Proxyfan.Domain.IDomainEventBus.md 'Proxyfan\.Domain\.IDomainEventBus') | Defines an in\-process domain event bus for publishing and subscribing to domain events across bounded contexts\. |
