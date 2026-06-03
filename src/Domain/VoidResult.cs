namespace Proxyfan.Domain;

/// <summary>
///     Represents the outcome of a domain operation that produces no value.
/// </summary>
public sealed class VoidResult
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
    ///     Initializes a new successful <see cref="VoidResult" />. Prefer <see cref="Result.Success()" /> to
    ///     obtain the shared success instance; failures must go through the <see cref="DomainError" />
    ///     constructor or <see cref="Result.Failure(DomainError)" /> so the <see cref="Error" /> property
    ///     is always populated when <see cref="IsSuccess" /> is <see langword="false" />.
    /// </summary>
    public VoidResult()
    {
        IsSuccess = true;
    }

    /// <summary>
    ///     Initializes a new failed <see cref="VoidResult" /> holding <paramref name="error" />.
    /// </summary>
    /// <param name="error">The domain error describing the failure.</param>
    public VoidResult(DomainError error)
    {
        Error = error;
        IsSuccess = false;
    }
}