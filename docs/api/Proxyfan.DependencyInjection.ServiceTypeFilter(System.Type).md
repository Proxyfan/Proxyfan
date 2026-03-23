### [Proxyfan\.DependencyInjection](Proxyfan.DependencyInjection.md 'Proxyfan\.DependencyInjection')

## ServiceTypeFilter\(Type\) Delegate

Delegate that determines whether a [System\.Type](https://learn.microsoft.com/en-us/dotnet/api/system.type 'System\.Type') should be registered with the DI container\.

```csharp
public delegate bool ServiceTypeFilter(System.Type type);
```
#### Parameters

<a name='Proxyfan.DependencyInjection.ServiceTypeFilter(System.Type).type'></a>

`type` [System\.Type](https://learn.microsoft.com/en-us/dotnet/api/system.type 'System\.Type')

The type to evaluate\.

#### Returns
[System\.Boolean](https://learn.microsoft.com/en-us/dotnet/api/system.boolean 'System\.Boolean')