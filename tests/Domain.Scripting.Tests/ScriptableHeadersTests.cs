using System.Collections.Generic;
using System.Threading.Tasks;

namespace Proxyfan.Domain.Scripting.Tests;

/// <summary>
///     Tests for <see cref="ScriptableHeaders" />.
/// </summary>
public sealed class ScriptableHeadersTests
{
    /// <summary>
    ///     Verifies that an empty collection has a count of zero.
    /// </summary>
    [Test]
    public async Task Count_Empty_IsZero()
    {
        var headers = new ScriptableHeaders();

        await Assert.That(headers.Count).IsEqualTo(0);
    }

    /// <summary>
    ///     Verifies that Set then Get round-trips a single value.
    /// </summary>
    [Test]
    public async Task Set_ThenGet_ReturnsValue()
    {
        var headers = new ScriptableHeaders();

        headers.Set("X-Test", "value");

        await Assert.That(headers.Get("X-Test")).IsEqualTo("value");
        await Assert.That(headers.HasHeader("X-Test")).IsTrue();
    }

    /// <summary>
    ///     Verifies that Set replaces an existing value.
    /// </summary>
    [Test]
    public async Task Set_TwiceWithSameName_ReplacesValue()
    {
        var headers = new ScriptableHeaders();
        headers.Set("X-Test", "v1");

        headers.Set("X-Test", "v2");

        await Assert.That(headers.Get("X-Test")).IsEqualTo("v2");
    }

    /// <summary>
    ///     Verifies that Add appends multiple values under the same header name.
    /// </summary>
    [Test]
    public async Task Add_TwiceWithSameName_AppendsValues()
    {
        var headers = new ScriptableHeaders();
        headers.Add("Set-Cookie", "a=1");
        headers.Add("Set-Cookie", "b=2");

        var values = new List<string>();
        foreach (var header in headers.Enumerate())
        {
            if (header.Key == "Set-Cookie")
            {
                values.Add(header.Value);
            }
        }

        await Assert.That(headers.Get("Set-Cookie")).IsEqualTo("a=1");
        await Assert.That(values.Count).IsEqualTo(2);
        await Assert.That(values[0]).IsEqualTo("a=1");
        await Assert.That(values[1]).IsEqualTo("b=2");
    }

    /// <summary>
    ///     Verifies that header name lookup is case-insensitive.
    /// </summary>
    [Test]
    public async Task Get_MixedCaseName_IsCaseInsensitive()
    {
        var headers = new ScriptableHeaders();
        headers.Set("Content-Type", "text/plain");

        await Assert.That(headers.Get("content-type")).IsEqualTo("text/plain");
    }

    /// <summary>
    ///     Verifies that <see cref="ScriptableHeaders.HasRemoved" /> removes a present header.
    /// </summary>
    [Test]
    public async Task HasRemoved_PresentHeader_ReturnsTrue()
    {
        var headers = new ScriptableHeaders();
        headers.Set("X-Test", "value");

        var removed = headers.HasRemoved("X-Test");

        await Assert.That(removed).IsTrue();
        await Assert.That(headers.HasHeader("X-Test")).IsFalse();
    }

    /// <summary>
    ///     Verifies that <see cref="ScriptableHeaders.HasRemoved" /> returns false for absent header.
    /// </summary>
    [Test]
    public async Task HasRemoved_AbsentHeader_ReturnsFalse()
    {
        var headers = new ScriptableHeaders();

        var removed = headers.HasRemoved("X-Missing");

        await Assert.That(removed).IsFalse();
    }

    /// <summary>
    ///     Verifies that Get returns null when the header is absent.
    /// </summary>
    [Test]
    public async Task Get_AbsentHeader_ReturnsNull()
    {
        var headers = new ScriptableHeaders();

        var value = headers.Get("X-Missing");

        await Assert.That(value).IsNull();
    }

    /// <summary>
    ///     Verifies that Enumerate returns all stored headers.
    /// </summary>
    [Test]
    public async Task Enumerate_AfterSet_ReturnsAllStoredHeaders()
    {
        var headers = new ScriptableHeaders();
        headers.Set("A", "1");
        headers.Set("B", "2");

        var count = 0;
        foreach (var _ in headers.Enumerate())
        {
            count++;
        }

        await Assert.That(count).IsEqualTo(2);
    }
}
