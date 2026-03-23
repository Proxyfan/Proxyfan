### [Proxyfan\.Presentation](Proxyfan.Presentation.md 'Proxyfan\.Presentation')

## ContainerLocator Class

Provides access to the application's DI container\. Intended for use in XAML bindings only —
do not use from application code\.

```csharp
public static class ContainerLocator
```

Inheritance [System\.Object](https://learn.microsoft.com/en-us/dotnet/api/system.object 'System\.Object') &#129106; ContainerLocator
### Properties

<a name='Proxyfan.Presentation.ContainerLocator.Current'></a>

## ContainerLocator\.Current Property

Gets the current [System\.IServiceProvider](https://learn.microsoft.com/en-us/dotnet/api/system.iserviceprovider 'System\.IServiceProvider'), or [null](https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/null 'https://docs\.microsoft\.com/en\-us/dotnet/csharp/language\-reference/keywords/null') if not yet initialized\.

```csharp
public static System.IServiceProvider? Current { get; }
```

#### Property Value
[System\.IServiceProvider](https://learn.microsoft.com/en-us/dotnet/api/system.iserviceprovider 'System\.IServiceProvider')
### Methods

<a name='Proxyfan.Presentation.ContainerLocator.Reset()'></a>

## ContainerLocator\.Reset\(\) Method

Resets the container to its uninitialized state\. For use in tests only\.

```csharp
public static void Reset();
```

<a name='Proxyfan.Presentation.ContainerLocator.Set(Proxyfan.Presentation.ServiceLocatorFactory)'></a>

## ContainerLocator\.Set\(ServiceLocatorFactory\) Method

Registers a factory that will be used to lazily resolve the [System\.IServiceProvider](https://learn.microsoft.com/en-us/dotnet/api/system.iserviceprovider 'System\.IServiceProvider')\.

```csharp
public static void Set(Proxyfan.Presentation.ServiceLocatorFactory factory);
```
#### Parameters

<a name='Proxyfan.Presentation.ContainerLocator.Set(Proxyfan.Presentation.ServiceLocatorFactory).factory'></a>

`factory` [ServiceLocatorFactory\(\)](Proxyfan.Presentation.ServiceLocatorFactory().md 'Proxyfan\.Presentation\.ServiceLocatorFactory\(\)')

A delegate that returns the application's [System\.IServiceProvider](https://learn.microsoft.com/en-us/dotnet/api/system.iserviceprovider 'System\.IServiceProvider')\.