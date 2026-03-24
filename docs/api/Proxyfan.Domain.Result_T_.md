### [Proxyfan\.Domain](Proxyfan.Domain.md 'Proxyfan\.Domain')

## Result\<T\> Class

Represents the outcome of a domain operation that produces a value of type
[T](Proxyfan.Domain.Result_T_.md#Proxyfan.Domain.Result_T_.T 'Proxyfan\.Domain\.Result\<T\>\.T')\.

```csharp
public sealed class Result<T>
```
#### Type parameters

<a name='Proxyfan.Domain.Result_T_.T'></a>

`T`

The type of the success value\.

Inheritance [System\.Object](https://learn.microsoft.com/en-us/dotnet/api/system.object 'System\.Object') &#129106; Result\<T\>
### Properties

<a name='Proxyfan.Domain.Result_T_.Error'></a>

## Result\<T\>\.Error Property

Gets the error when the operation failed, or [null](https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/null 'https://docs\.microsoft\.com/en\-us/dotnet/csharp/language\-reference/keywords/null') if it succeeded\.

```csharp
public Proxyfan.Domain.DomainError? Error { get; }
```

#### Property Value
[DomainError](Proxyfan.Domain.DomainError.md 'Proxyfan\.Domain\.DomainError')

<a name='Proxyfan.Domain.Result_T_.IsSuccess'></a>

## Result\<T\>\.IsSuccess Property

Gets a value indicating whether the operation succeeded\.

```csharp
public bool IsSuccess { get; }
```

#### Property Value
[System\.Boolean](https://learn.microsoft.com/en-us/dotnet/api/system.boolean 'System\.Boolean')

<a name='Proxyfan.Domain.Result_T_.Value'></a>

## Result\<T\>\.Value Property

Gets the success value\.

```csharp
public T Value { get; }
```

#### Property Value
[T](Proxyfan.Domain.Result_T_.md#Proxyfan.Domain.Result_T_.T 'Proxyfan\.Domain\.Result\<T\>\.T')

#### Exceptions

[System\.InvalidOperationException](https://learn.microsoft.com/en-us/dotnet/api/system.invalidoperationexception 'System\.InvalidOperationException')  
Thrown when the result represents a failure\.