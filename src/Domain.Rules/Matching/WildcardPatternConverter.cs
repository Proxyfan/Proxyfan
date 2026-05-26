using System.Text;
using System.Text.RegularExpressions;

namespace Proxyfan.Domain.Rules.Matching;

/// <summary>
///     Converts wildcard patterns (using <c>*</c> and <c>?</c>) into equivalent regular expressions.
/// </summary>
public static class WildcardPatternConverter
{
    /// <summary>
    ///     Converts a wildcard pattern into an anchored regular expression source string.
    /// </summary>
    /// <param name="pattern">The wildcard pattern to translate.</param>
    /// <returns>The translated regular expression pattern.</returns>
    public static string ConvertToRegexPattern(string pattern)
    {
        var builder = new StringBuilder();
        builder.Append('^');

        foreach (var character in pattern)
        {
            switch (character)
            {
                case '*':
                    builder.Append(".*");
                    break;
                case '?':
                    builder.Append('.');
                    break;
                default:
                    builder.Append(Regex.Escape(character.ToString()));
                    break;
            }
        }

        builder.Append('$');
        return builder.ToString();
    }
}
