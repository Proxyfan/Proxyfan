### [Proxyfan\.Domain](Proxyfan.Domain.md 'Proxyfan\.Domain')

## Result Class

Represents the outcome of a domain operation that produces no value\.
Also provides factory methods for creating [Result&lt;T&gt;](Proxyfan.Domain.Result_T_.md 'Proxyfan\.Domain\.Result\<T\>') instances\.

```csharp
public sealed class Result
```

Inheritance [System\.Object](https://learn.microsoft.com/en-us/dotnet/api/system.object 'System\.Object') &#129106; Result
### Properties

<a name='Proxyfan.Domain.Result.Error'></a>

## Result\.Error Property

Gets the error when the operation failed, or [null](https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/null 'https://docs\.microsoft\.com/en\-us/dotnet/csharp/language\-reference/keywords/null') if it succeeded\.

```csharp
public Proxyfan.Domain.DomainError? Error { get; }
```

#### Property Value
[DomainError](Proxyfan.Domain.DomainError.md 'Proxyfan\.Domain\.DomainError')

<a name='Proxyfan.Domain.Result.IsSuccess'></a>

## Result\.IsSuccess Property

Gets a value indicating whether the operation succeeded\.

```csharp
public bool IsSuccess { get; }
```

#### Property Value
[System\.Boolean](https://learn.microsoft.com/en-us/dotnet/api/system.boolean 'System\.Boolean')
### Methods

<a name='Proxyfan.Domain.Result.Failure(Proxyfan.Domain.DomainError)'></a>

## Result\.Failure\(DomainError\) Method

Creates a failed void result holding [error](Proxyfan.Domain.Result.md#Proxyfan.Domain.Result.Failure(Proxyfan.Domain.DomainError).error 'Proxyfan\.Domain\.Result\.Failure\(Proxyfan\.Domain\.DomainError\)\.error')\.

```csharp
public static Proxyfan.Domain.Result Failure(Proxyfan.Domain.DomainError error);
```
#### Parameters

<a name='Proxyfan.Domain.Result.Failure(Proxyfan.Domain.DomainError).error'></a>

`error` [DomainError](Proxyfan.Domain.DomainError.md 'Proxyfan\.Domain\.DomainError')

The domain error describing the failure\.

#### Returns
[Result](Proxyfan.Domain.Result.md 'Proxyfan\.Domain\.Result')  
A failed [Result](Proxyfan.Domain.Result.md 'Proxyfan\.Domain\.Result')\.

<a name='Proxyfan.Domain.Result.Failure_T_(Proxyfan.Domain.DomainError)'></a>

## Result\.Failure\<T\>\(DomainError\) Method

Creates a failed result holding [error](Proxyfan.Domain.Result.md#Proxyfan.Domain.Result.Failure_T_(Proxyfan.Domain.DomainError).error 'Proxyfan\.Domain\.Result\.Failure\<T\>\(Proxyfan\.Domain\.DomainError\)\.error')\.

```csharp
public static Proxyfan.Domain.Result<T> Failure<T>(Proxyfan.Domain.DomainError error);
```
#### Type parameters

<a name='Proxyfan.Domain.Result.Failure_T_(Proxyfan.Domain.DomainError).T'></a>

`T`

The type of the success value\.
#### Parameters

<a name='Proxyfan.Domain.Result.Failure_T_(Proxyfan.Domain.DomainError).error'></a>

`error` [DomainError](Proxyfan.Domain.DomainError.md 'Proxyfan\.Domain\.DomainError')

The domain error describing the failure\.

#### Returns
[Proxyfan\.Domain\.Result&lt;](Proxyfan.Domain.Result_T_.md 'Proxyfan\.Domain\.Result\<T\>')[T](Proxyfan.Domain.Result.md#Proxyfan.Domain.Result.Failure_T_(Proxyfan.Domain.DomainError).T 'Proxyfan\.Domain\.Result\.Failure\<T\>\(Proxyfan\.Domain\.DomainError\)\.T')[&gt;](Proxyfan.Domain.Result_T_.md 'Proxyfan\.Domain\.Result\<T\>')  
A failed [Result&lt;T&gt;](Proxyfan.Domain.Result_T_.md 'Proxyfan\.Domain\.Result\<T\>')\.

<a name='Proxyfan.Domain.Result.Success()'></a>

## Result\.Success\(\) Method

Creates a successful void result\.

```csharp
public static Proxyfan.Domain.Result Success();
```

#### Returns
[Result](Proxyfan.Domain.Result.md 'Proxyfan\.Domain\.Result')  
A successful [Result](Proxyfan.Domain.Result.md 'Proxyfan\.Domain\.Result')\.

<a name='Proxyfan.Domain.Result.Success_T_(T)'></a>

## Result\.Success\<T\>\(T\) Method

Creates a successful result holding [value](Proxyfan.Domain.Result.md#Proxyfan.Domain.Result.Success_T_(T).value 'Proxyfan\.Domain\.Result\.Success\<T\>\(T\)\.value')\.

```csharp
public static Proxyfan.Domain.Result<T> Success<T>(T value);
```
#### Type parameters

<a name='Proxyfan.Domain.Result.Success_T_(T).T'></a>

`T`

The type of the success value\.
#### Parameters

<a name='Proxyfan.Domain.Result.Success_T_(T).value'></a>

`value` [T](Proxyfan.Domain.Result.md#Proxyfan.Domain.Result.Success_T_(T).T 'Proxyfan\.Domain\.Result\.Success\<T\>\(T\)\.T')

The success value\.

#### Returns
[Proxyfan\.Domain\.Result&lt;](Proxyfan.Domain.Result_T_.md 'Proxyfan\.Domain\.Result\<T\>')[T](Proxyfan.Domain.Result.md#Proxyfan.Domain.Result.Success_T_(T).T 'Proxyfan\.Domain\.Result\.Success\<T\>\(T\)\.T')[&gt;](Proxyfan.Domain.Result_T_.md 'Proxyfan\.Domain\.Result\<T\>')  
A successful [Result&lt;T&gt;](Proxyfan.Domain.Result_T_.md 'Proxyfan\.Domain\.Result\<T\>')\.