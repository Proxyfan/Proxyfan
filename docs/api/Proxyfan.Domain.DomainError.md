### [Proxyfan\.Domain](Proxyfan.Domain.md 'Proxyfan\.Domain')

## DomainError Class

Base record for all domain errors, carrying a machine\-readable code and a
human\-readable message\.

```csharp
public abstract record DomainError : System.IEquatable<Proxyfan.Domain.DomainError>
```

Inheritance [System\.Object](https://learn.microsoft.com/en-us/dotnet/api/system.object 'System\.Object') &#129106; DomainError

Implements [System\.IEquatable&lt;](https://learn.microsoft.com/en-us/dotnet/api/system.iequatable-1 'System\.IEquatable\`1')[DomainError](Proxyfan.Domain.DomainError.md 'Proxyfan\.Domain\.DomainError')[&gt;](https://learn.microsoft.com/en-us/dotnet/api/system.iequatable-1 'System\.IEquatable\`1')
### Constructors

<a name='Proxyfan.Domain.DomainError.DomainError(string,string,System.Exception)'></a>

## DomainError\(string, string, Exception\) Constructor

Base record for all domain errors, carrying a machine\-readable code and a
human\-readable message\.

```csharp
protected DomainError(string Code, string Message, System.Exception? InnerException=null);
```
#### Parameters

<a name='Proxyfan.Domain.DomainError.DomainError(string,string,System.Exception).Code'></a>

`Code` [System\.String](https://learn.microsoft.com/en-us/dotnet/api/system.string 'System\.String')

Machine\-readable error code \(e\.g\., `"PROXY_BIND_FAILED"`\)\.

<a name='Proxyfan.Domain.DomainError.DomainError(string,string,System.Exception).Message'></a>

`Message` [System\.String](https://learn.microsoft.com/en-us/dotnet/api/system.string 'System\.String')

Human\-readable error description\.

<a name='Proxyfan.Domain.DomainError.DomainError(string,string,System.Exception).InnerException'></a>

`InnerException` [System\.Exception](https://learn.microsoft.com/en-us/dotnet/api/system.exception 'System\.Exception')

Optional underlying exception that caused this error\.
### Properties

<a name='Proxyfan.Domain.DomainError.Code'></a>

## DomainError\.Code Property

Machine\-readable error code \(e\.g\., `"PROXY_BIND_FAILED"`\)\.

```csharp
public string Code { get; init; }
```

#### Property Value
[System\.String](https://learn.microsoft.com/en-us/dotnet/api/system.string 'System\.String')

<a name='Proxyfan.Domain.DomainError.InnerException'></a>

## DomainError\.InnerException Property

Optional underlying exception that caused this error\.

```csharp
public System.Exception? InnerException { get; init; }
```

#### Property Value
[System\.Exception](https://learn.microsoft.com/en-us/dotnet/api/system.exception 'System\.Exception')

<a name='Proxyfan.Domain.DomainError.Message'></a>

## DomainError\.Message Property

Human\-readable error description\.

```csharp
public string Message { get; init; }
```

#### Property Value
[System\.String](https://learn.microsoft.com/en-us/dotnet/api/system.string 'System\.String')