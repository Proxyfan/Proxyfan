## Proxyfan\.Presentation Namespace

| Classes | |
| :--- | :--- |
| [ContainerLocator](Proxyfan.Presentation.ContainerLocator.md 'Proxyfan\.Presentation\.ContainerLocator') | Provides access to the application's DI container\. Intended for use in XAML bindings only — do not use from application code\. |
| [ViewModelLocator](Proxyfan.Presentation.ViewModelLocator.md 'Proxyfan\.Presentation\.ViewModelLocator') | An Avalonia attached property that resolves a ViewModel from the DI container and sets it as the `DataContext` of the target control\. |

| Delegates | |
| :--- | :--- |
| [ServiceLocatorFactory\(\)](Proxyfan.Presentation.ServiceLocatorFactory().md 'Proxyfan\.Presentation\.ServiceLocatorFactory\(\)') | Delegate that returns the application's [System\.IServiceProvider](https://learn.microsoft.com/en-us/dotnet/api/system.iserviceprovider 'System\.IServiceProvider') for use in lazy container initialization\. |
