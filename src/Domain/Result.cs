using System;

namespace Proxyfan.Domain;

/// <summary>
///     Represents the outcome of a domain operation that produces a value of type
///     <typeparamref name="T" />.
/// </summary>
/// <typeparam name="T">The type of the success value.</typeparam>
public sealed class Result<T>
{
    private T? SuccessValue { get; }

    private Result(T value)
    {
        SuccessValue = value;
        IsSuccess = true;
    }

    private Result(DomainError error)
    {
        Error = error;
        IsSuccess = false;
    }

    /// <summary>Gets a value indicating whether the operation succeeded.</summary>
    public bool IsSuccess { get; }

    /// <summary>Gets the error when the operation failed, or <see langword="null" /> if it succeeded.</summary>
    public DomainError? Error { get; }

    /// <summary>
    ///     Gets the success value.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when the result represents a failure.</exception>
    public T Value
    {
        get
        {
            if (!IsSuccess)
            {
                throw new InvalidOperationException(
                    $"Cannot access Value on a failed result. Error: {Error!.Code} — {Error.Message}");
            }

            return SuccessValue!;
        }
    }

    internal static Result<T> CreateSuccess(T value)
    {
        return new Result<T>(value);
    }

    internal static Result<T> CreateFailure(DomainError error)
    {
        return new Result<T>(error);
    }
}

/// <summary>
///     Represents the outcome of a domain operation that produces no value.
///     Also provides factory methods for creating <see cref="Result{T}" /> instances.
/// </summary>
public sealed class Result
{
    private static readonly Result SuccessInstance = new(isSuccess: true);

    private Result(bool isSuccess)
    {
        IsSuccess = isSuccess;
    }

    private Result(DomainError error)
    {
        Error = error;
        IsSuccess = false;
    }

    /// <summary>Gets a value indicating whether the operation succeeded.</summary>
    public bool IsSuccess { get; }

    /// <summary>Gets the error when the operation failed, or <see langword="null" /> if it succeeded.</summary>
    public DomainError? Error { get; }

    /// <summary>Creates a successful void result.</summary>
    /// <returns>A successful <see cref="Result" />.</returns>
    public static Result Success()
    {
        return SuccessInstance;
    }

    /// <summary>Creates a successful result holding <paramref name="value" />.</summary>
    /// <typeparam name="T">The type of the success value.</typeparam>
    /// <param name="value">The success value.</param>
    /// <returns>A successful <see cref="Result{T}" />.</returns>
    public static Result<T> Success<T>(T value)
    {
        return Result<T>.CreateSuccess(value);
    }

    /// <summary>Creates a failed void result holding <paramref name="error" />.</summary>
    /// <param name="error">The domain error describing the failure.</param>
    /// <returns>A failed <see cref="Result" />.</returns>
    public static Result Failure(DomainError error)
    {
        return new Result(error);
    }

    /// <summary>Creates a failed result holding <paramref name="error" />.</summary>
    /// <typeparam name="T">The type of the success value.</typeparam>
    /// <param name="error">The domain error describing the failure.</param>
    /// <returns>A failed <see cref="Result{T}" />.</returns>
    public static Result<T> Failure<T>(DomainError error)
    {
        return Result<T>.CreateFailure(error);
    }
}
