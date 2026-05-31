namespace Proxyfan.Cli.Tests;

/// <summary>
///     Helpers that construct argument arrays from local variables, avoiding the CA1861
///     warning emitted when constant array literals are passed directly to method calls.
/// </summary>
public static class ParserTestArguments
{
    /// <summary>
    ///     Builds a single-element string array from a non-constant token.
    /// </summary>
    /// <param name="token">The token to wrap.</param>
    /// <returns>The single-element array.</returns>
    public static string[] One(string token)
    {
        return new[] { token };
    }

    /// <summary>
    ///     Builds a three-element string array.
    /// </summary>
    /// <param name="first">The first element.</param>
    /// <param name="second">The second element.</param>
    /// <param name="third">The third element.</param>
    /// <returns>The constructed array.</returns>
    public static string[] Three(string first, string second, string third)
    {
        return new[] { first, second, third };
    }

    /// <summary>
    ///     Builds a two-element string array.
    /// </summary>
    /// <param name="first">The first element.</param>
    /// <param name="second">The second element.</param>
    /// <returns>The constructed array.</returns>
    public static string[] Two(string first, string second)
    {
        return new[] { first, second };
    }

    /// <summary>
    ///     Builds a five-element string array.
    /// </summary>
    /// <param name="first">The first element.</param>
    /// <param name="second">The second element.</param>
    /// <param name="third">The third element.</param>
    /// <param name="fourth">The fourth element.</param>
    /// <param name="fifth">The fifth element.</param>
    /// <returns>The constructed array.</returns>
    public static string[] Five(string first, string second, string third, string fourth, string fifth)
    {
        return new[] { first, second, third, fourth, fifth };
    }

    /// <summary>
    ///     Builds a four-element string array.
    /// </summary>
    /// <param name="first">The first element.</param>
    /// <param name="second">The second element.</param>
    /// <param name="third">The third element.</param>
    /// <param name="fourth">The fourth element.</param>
    /// <returns>The constructed array.</returns>
    public static string[] Four(string first, string second, string third, string fourth)
    {
        return new[] { first, second, third, fourth };
    }
}
