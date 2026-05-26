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
    ///     Initializes a new <see cref="VoidResult" /> with the specified success state.
    /// </summary>
    /// <param name="isSuccess"><see langword="true" /> for a successful result; <see langword="false" /> for failure.</param>
    public VoidResult(bool isSuccess)
    {
        IsSuccess = isSuccess;
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