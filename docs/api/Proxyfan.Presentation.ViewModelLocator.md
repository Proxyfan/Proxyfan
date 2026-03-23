### [Proxyfan\.Presentation](Proxyfan.Presentation.md 'Proxyfan\.Presentation')

## ViewModelLocator Class

An Avalonia attached property that resolves a ViewModel from the DI container and sets it
as the `DataContext` of the target control\.

```csharp
public static class ViewModelLocator
```

Inheritance [System\.Object](https://learn.microsoft.com/en-us/dotnet/api/system.object 'System\.Object') &#129106; ViewModelLocator
### Fields

<a name='Proxyfan.Presentation.ViewModelLocator.DataContextProperty'></a>

## ViewModelLocator\.DataContextProperty Field

Identifies the `DataContext` attached property, whose value is the
[System\.Type](https://learn.microsoft.com/en-us/dotnet/api/system.type 'System\.Type') of the ViewModel to resolve from the DI container\.

```csharp
public static readonly AttachedProperty<Type?> DataContextProperty;
```

#### Field Value
[Avalonia\.AttachedProperty](https://learn.microsoft.com/en-us/dotnet/api/avalonia.attachedproperty 'Avalonia\.AttachedProperty')
### Methods

<a name='Proxyfan.Presentation.ViewModelLocator.GetDataContext(Control)'></a>

## ViewModelLocator\.GetDataContext\(Control\) Method

Gets the ViewModel type assigned to [element](Proxyfan.Presentation.ViewModelLocator.md#Proxyfan.Presentation.ViewModelLocator.GetDataContext(Control).element 'Proxyfan\.Presentation\.ViewModelLocator\.GetDataContext\(Control\)\.element')\.

```csharp
public static System.Type? GetDataContext(Control element);
```
#### Parameters

<a name='Proxyfan.Presentation.ViewModelLocator.GetDataContext(Control).element'></a>

`element` [Avalonia\.Controls\.Control](https://learn.microsoft.com/en-us/dotnet/api/avalonia.controls.control 'Avalonia\.Controls\.Control')

The control to query\.

#### Returns
[System\.Type](https://learn.microsoft.com/en-us/dotnet/api/system.type 'System\.Type')  
The assigned [System\.Type](https://learn.microsoft.com/en-us/dotnet/api/system.type 'System\.Type'), or [null](https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/null 'https://docs\.microsoft\.com/en\-us/dotnet/csharp/language\-reference/keywords/null')\.

<a name='Proxyfan.Presentation.ViewModelLocator.SetDataContext(Control,System.Type)'></a>

## ViewModelLocator\.SetDataContext\(Control, Type\) Method

Sets the ViewModel type on [element](Proxyfan.Presentation.ViewModelLocator.md#Proxyfan.Presentation.ViewModelLocator.SetDataContext(Control,System.Type).element 'Proxyfan\.Presentation\.ViewModelLocator\.SetDataContext\(Control, System\.Type\)\.element'), causing the DI container to be queried\.

```csharp
public static void SetDataContext(Control element, System.Type? value);
```
#### Parameters

<a name='Proxyfan.Presentation.ViewModelLocator.SetDataContext(Control,System.Type).element'></a>

`element` [Avalonia\.Controls\.Control](https://learn.microsoft.com/en-us/dotnet/api/avalonia.controls.control 'Avalonia\.Controls\.Control')

The control to configure\.

<a name='Proxyfan.Presentation.ViewModelLocator.SetDataContext(Control,System.Type).value'></a>

`value` [System\.Type](https://learn.microsoft.com/en-us/dotnet/api/system.type 'System\.Type')

The [System\.Type](https://learn.microsoft.com/en-us/dotnet/api/system.type 'System\.Type') of the ViewModel to resolve\.