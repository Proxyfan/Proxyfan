### [Proxyfan\.DependencyInjection](Proxyfan.DependencyInjection.md 'Proxyfan\.DependencyInjection')

## ServiceCollectionExtensions Class

Extension methods for [Microsoft\.Extensions\.DependencyInjection\.IServiceCollection](https://learn.microsoft.com/en-us/dotnet/api/microsoft.extensions.dependencyinjection.iservicecollection 'Microsoft\.Extensions\.DependencyInjection\.IServiceCollection')
that register types against every interface they implement\.

```csharp
public static class ServiceCollectionExtensions
```

Inheritance [System\.Object](https://learn.microsoft.com/en-us/dotnet/api/system.object 'System\.Object') &#129106; ServiceCollectionExtensions
### Methods

<a name='Proxyfan.DependencyInjection.ServiceCollectionExtensions.AddSingletonAsImplementedInterfaces_TImplementation_(thisMicrosoft.Extensions.DependencyInjection.IServiceCollection,System.Func_TImplementation_)'></a>

## ServiceCollectionExtensions\.AddSingletonAsImplementedInterfaces\<TImplementation\>\(this IServiceCollection, Func\<TImplementation\>\) Method

Registers [implementation](Proxyfan.DependencyInjection.ServiceCollectionExtensions.md#Proxyfan.DependencyInjection.ServiceCollectionExtensions.AddSingletonAsImplementedInterfaces_TImplementation_(thisMicrosoft.Extensions.DependencyInjection.IServiceCollection,System.Func_TImplementation_).implementation 'Proxyfan\.DependencyInjection\.ServiceCollectionExtensions\.AddSingletonAsImplementedInterfaces\<TImplementation\>\(this Microsoft\.Extensions\.DependencyInjection\.IServiceCollection, System\.Func\<TImplementation\>\)\.implementation') as a singleton against every interface it implements\.

```csharp
public static void AddSingletonAsImplementedInterfaces<TImplementation>(this Microsoft.Extensions.DependencyInjection.IServiceCollection serviceCollection, System.Func<TImplementation> implementation)
    where TImplementation : notnull;
```
#### Type parameters

<a name='Proxyfan.DependencyInjection.ServiceCollectionExtensions.AddSingletonAsImplementedInterfaces_TImplementation_(thisMicrosoft.Extensions.DependencyInjection.IServiceCollection,System.Func_TImplementation_).TImplementation'></a>

`TImplementation`

The concrete type of the implementation instance\.
#### Parameters

<a name='Proxyfan.DependencyInjection.ServiceCollectionExtensions.AddSingletonAsImplementedInterfaces_TImplementation_(thisMicrosoft.Extensions.DependencyInjection.IServiceCollection,System.Func_TImplementation_).serviceCollection'></a>

`serviceCollection` [Microsoft\.Extensions\.DependencyInjection\.IServiceCollection](https://learn.microsoft.com/en-us/dotnet/api/microsoft.extensions.dependencyinjection.iservicecollection 'Microsoft\.Extensions\.DependencyInjection\.IServiceCollection')

<a name='Proxyfan.DependencyInjection.ServiceCollectionExtensions.AddSingletonAsImplementedInterfaces_TImplementation_(thisMicrosoft.Extensions.DependencyInjection.IServiceCollection,System.Func_TImplementation_).implementation'></a>

`implementation` [System\.Func&lt;](https://learn.microsoft.com/en-us/dotnet/api/system.func-1 'System\.Func\`1')[TImplementation](Proxyfan.DependencyInjection.ServiceCollectionExtensions.md#Proxyfan.DependencyInjection.ServiceCollectionExtensions.AddSingletonAsImplementedInterfaces_TImplementation_(thisMicrosoft.Extensions.DependencyInjection.IServiceCollection,System.Func_TImplementation_).TImplementation 'Proxyfan\.DependencyInjection\.ServiceCollectionExtensions\.AddSingletonAsImplementedInterfaces\<TImplementation\>\(this Microsoft\.Extensions\.DependencyInjection\.IServiceCollection, System\.Func\<TImplementation\>\)\.TImplementation')[&gt;](https://learn.microsoft.com/en-us/dotnet/api/system.func-1 'System\.Func\`1')

The singleton instance to register\.