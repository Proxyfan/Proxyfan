### [Proxyfan\.Domain\.Proxy](Proxyfan.Domain.Proxy.md 'Proxyfan\.Domain\.Proxy')

## ProxyOptions Class

Strongly\-typed configuration options for the proxy listener, bound from the `proxy`
section of the application configuration\.

```csharp
public sealed class ProxyOptions
```

Inheritance [System\.Object](https://learn.microsoft.com/en-us/dotnet/api/system.object 'System\.Object') &#129106; ProxyOptions
### Fields

<a name='Proxyfan.Domain.Proxy.ProxyOptions.SectionKey'></a>

## ProxyOptions\.SectionKey Field

The configuration section key used when binding these options\.

```csharp
public const string SectionKey = "proxy";
```

#### Field Value
[System\.String](https://learn.microsoft.com/en-us/dotnet/api/system.string 'System\.String')
### Properties

<a name='Proxyfan.Domain.Proxy.ProxyOptions.AutoStart'></a>

## ProxyOptions\.AutoStart Property

Gets or sets a value indicating whether the proxy should start automatically on application launch\. Default: [true](https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/builtin-types/bool 'https://docs\.microsoft\.com/en\-us/dotnet/csharp/language\-reference/builtin\-types/bool')\.

```csharp
public bool AutoStart { get; set; }
```

#### Property Value
[System\.Boolean](https://learn.microsoft.com/en-us/dotnet/api/system.boolean 'System\.Boolean')

<a name='Proxyfan.Domain.Proxy.ProxyOptions.MaxConnections'></a>

## ProxyOptions\.MaxConnections Property

Gets or sets the maximum number of concurrent connections the listener accepts\. Default: 1000\.

```csharp
public int MaxConnections { get; set; }
```

#### Property Value
[System\.Int32](https://learn.microsoft.com/en-us/dotnet/api/system.int32 'System\.Int32')

<a name='Proxyfan.Domain.Proxy.ProxyOptions.Port'></a>

## ProxyOptions\.Port Property

Gets or sets the TCP port the proxy listener binds to\. Valid range: 1024–65535\. Default: 8080\.

```csharp
public int Port { get; set; }
```

#### Property Value
[System\.Int32](https://learn.microsoft.com/en-us/dotnet/api/system.int32 'System\.Int32')

<a name='Proxyfan.Domain.Proxy.ProxyOptions.RegisterSystemProxy'></a>

## ProxyOptions\.RegisterSystemProxy Property

Gets or sets a value indicating whether the proxy registers itself as the system proxy on Windows\. Default: [true](https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/builtin-types/bool 'https://docs\.microsoft\.com/en\-us/dotnet/csharp/language\-reference/builtin\-types/bool')\.

```csharp
public bool RegisterSystemProxy { get; set; }
```

#### Property Value
[System\.Boolean](https://learn.microsoft.com/en-us/dotnet/api/system.boolean 'System\.Boolean')