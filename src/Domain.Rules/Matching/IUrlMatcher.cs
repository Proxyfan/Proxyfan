namespace Proxyfan.Domain.Rules.Matching;

/// <summary>
///     Defines a contract for evaluating whether a target URL matches a pattern.
/// </summary>
public interface IUrlMatcher
{
    /// <summary>
    ///     Evaluates the supplied URL against the underlying pattern.
    /// </summary>
    /// <param name="url">
    ///     The fully-qualified URL to evaluate. The matcher decides which portions to inspect.
    /// </param>
    /// <returns>
    ///     The outcome of the match attempt.
    /// </returns>
    UrlMatchResult GetMatchResult(string url);

    /// <summary>
    ///     Determines whether the supplied URL has a successful match against the underlying
    ///     pattern.
    /// </summary>
    /// <param name="url">
    ///     The fully-qualified URL to evaluate. The matcher decides which portions to inspect.
    /// </param>
    /// <returns>
    ///     <see langword="true" /> when the URL matches the pattern; otherwise
    ///     <see langword="false" />.
    /// </returns>
    bool HasMatch(string url);
}
