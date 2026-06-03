using System.Text;
using System.Threading.Tasks;
using Proxyfan.Domain.Traffic;

namespace Proxyfan.Domain.Traffic.Tests;

/// <summary>
///     Tests for <see cref="FormUrlEncodedParser" />.
/// </summary>
public sealed class FormUrlEncodedParserTests
{
    /// <summary>
    ///     Verifies that an empty body yields an empty list.
    /// </summary>
    [Test]
    public async Task Parse_EmptyBody_ReturnsEmpty()
    {
        var result = FormUrlEncodedParser.Parse(System.ReadOnlyMemory<byte>.Empty);

        await Assert.That(result.Count).IsEqualTo(0);
    }

    /// <summary>
    ///     Verifies that an empty string yields an empty list.
    /// </summary>
    [Test]
    public async Task Parse_EmptyString_ReturnsEmpty()
    {
        var result = FormUrlEncodedParser.Parse(string.Empty);

        await Assert.That(result.Count).IsEqualTo(0);
    }

    /// <summary>
    ///     Verifies that name/value pairs are decoded.
    /// </summary>
    [Test]
    public async Task Parse_NameValuePairs_DecodesValues()
    {
        var body = Encoding.UTF8.GetBytes("username=alice&password=p%40ss+word");

        var result = FormUrlEncodedParser.Parse(body);

        await Assert.That(result.Count).IsEqualTo(2);
        await Assert.That(result[0].Name).IsEqualTo("username");
        await Assert.That(result[0].Value).IsEqualTo("alice");
        await Assert.That(result[1].Name).IsEqualTo("password");
        await Assert.That(result[1].Value).IsEqualTo("p@ss word");
    }

    /// <summary>
    ///     Verifies that the string overload reaches the same parser.
    /// </summary>
    [Test]
    public async Task Parse_StringInput_DecodesPairs()
    {
        var result = FormUrlEncodedParser.Parse("a=1&b=2");

        await Assert.That(result.Count).IsEqualTo(2);
        await Assert.That(result[1].Value).IsEqualTo("2");
    }

    /// <summary>
    ///     Verifies that a leading <c>?</c> in a form body is preserved as part of the first
    ///     field name and not treated as a URI query delimiter.
    /// </summary>
    [Test]
    public async Task Parse_StringStartingWithQuestionMark_PreservesQuestionMarkInName()
    {
        var result = FormUrlEncodedParser.Parse("?token=abc&b=2");

        await Assert.That(result.Count).IsEqualTo(2);
        await Assert.That(result[0].Name).IsEqualTo("?token");
        await Assert.That(result[0].Value).IsEqualTo("abc");
        await Assert.That(result[1].Name).IsEqualTo("b");
        await Assert.That(result[1].Value).IsEqualTo("2");
    }

    /// <summary>
    ///     Verifies that a leading <c>?</c> in a form body byte payload is preserved.
    /// </summary>
    [Test]
    public async Task Parse_BytesStartingWithQuestionMark_PreservesQuestionMarkInName()
    {
        var body = Encoding.UTF8.GetBytes("?token=abc");

        var result = FormUrlEncodedParser.Parse(body);

        await Assert.That(result.Count).IsEqualTo(1);
        await Assert.That(result[0].Name).IsEqualTo("?token");
        await Assert.That(result[0].Value).IsEqualTo("abc");
    }
}
