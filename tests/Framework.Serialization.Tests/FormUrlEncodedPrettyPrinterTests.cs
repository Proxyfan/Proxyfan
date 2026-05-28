using Proxyfan.Framework.Serialization;
using System.Threading.Tasks;
using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.Core;

namespace Proxyfan.Framework.Serialization.Tests;

/// <summary>
///     Tests for <see cref="FormUrlEncodedPrettyPrinter" />.
/// </summary>
public sealed class FormUrlEncodedPrettyPrinterTests
{
    [Test]
    public async Task PrettyPrint_Empty_ReturnsEmpty()
    {
        var result = FormUrlEncodedPrettyPrinter.PrettyPrint(string.Empty);

        await Assert.That(result).IsEqualTo(string.Empty);
    }

    [Test]
    public async Task PrettyPrint_SinglePair_FormatsAsKeyValue()
    {
        var result = FormUrlEncodedPrettyPrinter.PrettyPrint("name=alice");

        await Assert.That(result).IsEqualTo("name: alice");
    }

    [Test]
    public async Task PrettyPrint_MultiplePairs_SeparatesWithNewlines()
    {
        var result = FormUrlEncodedPrettyPrinter.PrettyPrint("name=alice&age=30");

        await Assert.That(result).IsEqualTo("name: alice\nage: 30");
    }

    [Test]
    public async Task PrettyPrint_PercentEncodedValue_Decodes()
    {
        var result = FormUrlEncodedPrettyPrinter.PrettyPrint("q=hello%20world");

        await Assert.That(result).IsEqualTo("q: hello world");
    }

    [Test]
    public async Task PrettyPrint_PlusEncodedSpace_DecodesAsSpace()
    {
        var result = FormUrlEncodedPrettyPrinter.PrettyPrint("q=hello+world");

        await Assert.That(result).IsEqualTo("q: hello world");
    }

    [Test]
    public async Task PrettyPrint_KeyWithoutValue_AppendsColon()
    {
        var result = FormUrlEncodedPrettyPrinter.PrettyPrint("flag");

        await Assert.That(result).IsEqualTo("flag:");
    }

    [Test]
    public async Task PrettyPrint_EmptyPair_IsSkipped()
    {
        var result = FormUrlEncodedPrettyPrinter.PrettyPrint("a=1&&b=2");

        await Assert.That(result).IsEqualTo("a: 1\nb: 2");
    }
}
