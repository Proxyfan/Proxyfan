#### [Client](index.md 'index')
### [Proxyfan\.Client](Proxyfan.Client.md 'Proxyfan\.Client')

## HostExtensions Class

Extension methods for [Microsoft\.Extensions\.Hosting\.IHost](https://learn.microsoft.com/en-us/dotnet/api/microsoft.extensions.hosting.ihost 'Microsoft\.Extensions\.Hosting\.IHost') to provide additional functionality, such as a synchronous Stop method\.

```csharp
public static class HostExtensions
```

Inheritance [System\.Object](https://learn.microsoft.com/en-us/dotnet/api/system.object 'System\.Object') &#129106; HostExtensions
### Methods

<a name='Proxyfan.Client.HostExtensions.Stop(thisMicrosoft.Extensions.Hosting.IHost)'></a>

## HostExtensions\.Stop\(this IHost\) Method

Stops the host synchronously by calling StopAsync and blocking until it completes\.

```csharp
public static void Stop(this Microsoft.Extensions.Hosting.IHost host);
```
#### Parameters

<a name='Proxyfan.Client.HostExtensions.Stop(thisMicrosoft.Extensions.Hosting.IHost).host'></a>

`host` [Microsoft\.Extensions\.Hosting\.IHost](https://learn.microsoft.com/en-us/dotnet/api/microsoft.extensions.hosting.ihost 'Microsoft\.Extensions\.Hosting\.IHost')