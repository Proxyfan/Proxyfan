### [Proxyfan\.Domain\.Traffic](Proxyfan.Domain.Traffic.md 'Proxyfan\.Domain\.Traffic')

## TrafficFlowStatus Enum

Represents the lifecycle status of a [TrafficFlow](Proxyfan.Domain.Traffic.TrafficFlow.md 'Proxyfan\.Domain\.Traffic\.TrafficFlow')\.

```csharp
public enum TrafficFlowStatus
```
### Fields

<a name='Proxyfan.Domain.Traffic.TrafficFlowStatus.Pending'></a>

`Pending` 0

The flow has been created but no data has been exchanged yet\.

<a name='Proxyfan.Domain.Traffic.TrafficFlowStatus.Active'></a>

`Active` 1

The flow is actively transferring data\.

<a name='Proxyfan.Domain.Traffic.TrafficFlowStatus.Completed'></a>

`Completed` 2

The flow completed successfully\.

<a name='Proxyfan.Domain.Traffic.TrafficFlowStatus.Failed'></a>

`Failed` 3

The flow terminated due to an error\.