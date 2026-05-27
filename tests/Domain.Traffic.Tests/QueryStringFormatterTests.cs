using System.Collections.Generic;
using System.Threading.Tasks;
using Proxyfan.Domain.Traffic;

namespace Proxyfan.Domain.Traffic.Tests;

/// <summary>
///     Tests for <see cref="QueryStringFormatter" />.
/// </summary>
public sealed class QueryStringFormatterTests
{
    /// <summary>
    ///     Verifies that a null list yields an empty string.
    /// </summary>
    [Test]
    public async Task Format_NullList_ReturnsEmptyString()
    {
        var result = QueryStringFormatter.Format(null);

        await Assert.That(result).IsEqualTo(string.Empty);
    }

    /// <summary>
    ///     Verifies that an empty list yields an empty string.
    /// </summary>
    [Test]
    public async Task Format_EmptyList_ReturnsEmptyString()
    {
        var parameters = new List<QueryParameter>();

        var result = QueryStringFormatter.Format(parameters);

        await Assert.That(result).IsEqualTo(string.Empty);
    }

    /// <summary>
    ///     Verifies that the table header is included.
    /// </summary>
    [Test]
    public async Task Format_SingleParameter_IncludesHeaderAndRow()
    {
        var parameters = new List<QueryParameter>
        {
            new QueryParameter("foo", "bar"),
        };

        var result = QueryStringFormatter.Format(parameters);

        await Assert.That(result.Contains("Name", System.StringComparison.Ordinal)).IsTrue();
        await Assert.That(result.Contains("Value", System.StringComparison.Ordinal)).IsTrue();
        await Assert.That(result.Contains("foo", System.StringComparison.Ordinal)).IsTrue();
        await Assert.That(result.Contains("bar", System.StringComparison.Ordinal)).IsTrue();
    }

    /// <summary>
    ///     Verifies that the longest value sets the column width.
    /// </summary>
    [Test]
    public async Task Format_LongestValueSetsWidth_LinesAlign()
    {
        var parameters = new List<QueryParameter>
        {
            new QueryParameter("a", "short"),
            new QueryParameter("very-long-name", "x"),
        };

        var result = QueryStringFormatter.Format(parameters);
        var lines = result.Split('\n', System.StringSplitOptions.RemoveEmptyEntries);

        await Assert.That(lines.Length).IsGreaterThan(2);
    }
}
