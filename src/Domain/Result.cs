using System;
using System.Threading;

namespace Proxyfan.Domain;

/// <summary>
///     Represents the outcome of a domain operation that produces a value of type
///     <typeparamref name="T" />.
/// </summary>
/// <typeparam name="T">The type of the success value.</typeparam>
public sealed class Result<T>
{
    /// <summary>
    ///     Gets the error when the operation failed, or <see langword="null" /> if it succeeded.
    /// </summary>
    public DomainError? Error { get; }

    /// <summary>
    ///     Gets a value indicating whether the operation succeeded.
    /// </summary>
    public bool IsSuccess { get; }

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

            return StoredValue!;
        }
    }

    private T? StoredValue { get; }

    /// <summary>
    ///     Initializes a new successful <see cref="Result{T}" /> holding <paramref name="value" />.
    /// </summary>
    /// <param name="value">The success value.</param>
    public Result(T value)
    {
        StoredValue = value;
        IsSuccess = true;
    }

    /// <summary>
    ///     Initializes a new failed <see cref="Result{T}" /> holding <paramref name="error" />.
    /// </summary>
    /// <param name="error">The domain error describing the failure.</param>
    public Result(DomainError error)
    {
        Error = error;
        IsSuccess = false;
    }
}

/// <summary>
///     Provides factory methods for creating <see cref="VoidResult" /> and <see cref="Result{T}" /> instances.
/// </summary>
public static class Result
{
    private static VoidResult? _voidSuccess;

    /// <summary>
    ///     Creates a failed void result holding <paramref name="error" />.
    /// </summary>
    /// <param name="error">The domain error describing the failure.</param>
    /// <returns>A failed <see cref="VoidResult" />.</returns>
    public static VoidResult Failure(DomainError error)
    {
        var result = new VoidResult(error);
        return result;
    }

    /// <summary>
    ///     Creates a failed result holding <paramref name="error" />.
    /// </summary>
    /// <typeparam name="T">The type of the success value.</typeparam>
    /// <param name="error">The domain error describing the failure.</param>
    /// <returns>A failed <see cref="Result{T}" />.</returns>
    public static Result<T> Failure<T>(DomainError error)
    {
        var result = new Result<T>(error);
        return result;
    }

    /// <summary>
    ///     Creates a successful void result. Returns the same singleton instance on every call.
    /// </summary>
    /// <returns>A successful <see cref="VoidResult" />.</returns>
    public static VoidResult Success()
    {
        if (_voidSuccess is not null)
        {
            return _voidSuccess;
        }

        var success = new VoidResult(isSuccess: true);
        Interlocked.CompareExchange(ref _voidSuccess, success, null);
        return _voidSuccess;
    }

    /// <summary>
    ///     Creates a successful result holding <paramref name="value" />.
    /// </summary>
    /// <typeparam name="T">The type of the success value.</typeparam>
    /// <param name="value">The success value.</param>
    /// <returns>A successful <see cref="Result{T}" />.</returns>
    public static Result<T> Success<T>(T value)
    {
        var result = new Result<T>(value);
        return result;
    }
}