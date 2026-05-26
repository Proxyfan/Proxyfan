namespace Proxyfan.Domain.Rules;

/// <summary>
///     Represents a rule-engine-related domain error.
/// </summary>
public sealed record RuleError : DomainError
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="RuleError" /> record.
    /// </summary>
    /// <param name="code">The machine-readable error code.</param>
    /// <param name="message">The human-readable error description.</param>
    public RuleError(string code, string message)
        : base(code, message)
    {
    }
}
