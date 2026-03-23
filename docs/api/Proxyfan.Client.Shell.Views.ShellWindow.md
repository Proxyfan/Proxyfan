#### [Client](index.md 'index')
### [Proxyfan\.Client\.Shell\.Views](Proxyfan.Client.Shell.Views.md 'Proxyfan\.Client\.Shell\.Views')

## ShellWindow Class

The main application window hosting the shell content for desktop platforms\.

```csharp
public class ShellWindow : Avalonia.Controls.Window
```

Inheritance [System\.Object](https://learn.microsoft.com/en-us/dotnet/api/system.object 'System\.Object') &#129106; [Avalonia\.AvaloniaObject](https://learn.microsoft.com/en-us/dotnet/api/avalonia.avaloniaobject 'Avalonia\.AvaloniaObject') &#129106; [Avalonia\.Animation\.Animatable](https://learn.microsoft.com/en-us/dotnet/api/avalonia.animation.animatable 'Avalonia\.Animation\.Animatable') &#129106; [Avalonia\.StyledElement](https://learn.microsoft.com/en-us/dotnet/api/avalonia.styledelement 'Avalonia\.StyledElement') &#129106; [Avalonia\.Visual](https://learn.microsoft.com/en-us/dotnet/api/avalonia.visual 'Avalonia\.Visual') &#129106; [Avalonia\.Layout\.Layoutable](https://learn.microsoft.com/en-us/dotnet/api/avalonia.layout.layoutable 'Avalonia\.Layout\.Layoutable') &#129106; [Avalonia\.Interactivity\.Interactive](https://learn.microsoft.com/en-us/dotnet/api/avalonia.interactivity.interactive 'Avalonia\.Interactivity\.Interactive') &#129106; [Avalonia\.Input\.InputElement](https://learn.microsoft.com/en-us/dotnet/api/avalonia.input.inputelement 'Avalonia\.Input\.InputElement') &#129106; [Avalonia\.Controls\.Control](https://learn.microsoft.com/en-us/dotnet/api/avalonia.controls.control 'Avalonia\.Controls\.Control') &#129106; [Avalonia\.Controls\.Primitives\.TemplatedControl](https://learn.microsoft.com/en-us/dotnet/api/avalonia.controls.primitives.templatedcontrol 'Avalonia\.Controls\.Primitives\.TemplatedControl') &#129106; [Avalonia\.Controls\.ContentControl](https://learn.microsoft.com/en-us/dotnet/api/avalonia.controls.contentcontrol 'Avalonia\.Controls\.ContentControl') &#129106; [Avalonia\.Controls\.TopLevel](https://learn.microsoft.com/en-us/dotnet/api/avalonia.controls.toplevel 'Avalonia\.Controls\.TopLevel') &#129106; [Avalonia\.Controls\.WindowBase](https://learn.microsoft.com/en-us/dotnet/api/avalonia.controls.windowbase 'Avalonia\.Controls\.WindowBase') &#129106; [Avalonia\.Controls\.Window](https://learn.microsoft.com/en-us/dotnet/api/avalonia.controls.window 'Avalonia\.Controls\.Window') &#129106; ShellWindow
### Constructors

<a name='Proxyfan.Client.Shell.Views.ShellWindow.ShellWindow()'></a>

## ShellWindow\(\) Constructor

Initializes a new instance of [ShellWindow](Proxyfan.Client.Shell.Views.ShellWindow.md 'Proxyfan\.Client\.Shell\.Views\.ShellWindow')\.

```csharp
public ShellWindow();
```
### Methods

<a name='Proxyfan.Client.Shell.Views.ShellWindow.InitializeComponent(bool,bool)'></a>

## ShellWindow\.InitializeComponent\(bool, bool\) Method

Wires up the controls and optionally loads XAML markup and attaches dev tools \(if Avalonia\.Diagnostics package is referenced\)\.

```csharp
public void InitializeComponent(bool loadXaml=true, bool attachDevTools=true);
```
#### Parameters

<a name='Proxyfan.Client.Shell.Views.ShellWindow.InitializeComponent(bool,bool).loadXaml'></a>

`loadXaml` [System\.Boolean](https://learn.microsoft.com/en-us/dotnet/api/system.boolean 'System\.Boolean')

Should the XAML be loaded into the component\.

<a name='Proxyfan.Client.Shell.Views.ShellWindow.InitializeComponent(bool,bool).attachDevTools'></a>

`attachDevTools` [System\.Boolean](https://learn.microsoft.com/en-us/dotnet/api/system.boolean 'System\.Boolean')

Should the dev tools be attached\.