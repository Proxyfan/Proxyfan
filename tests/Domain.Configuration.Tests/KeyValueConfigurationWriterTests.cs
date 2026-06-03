using System.Collections.Generic;
using System.Threading.Tasks;
using Proxyfan.Domain.Configuration;

namespace Proxyfan.Domain.Configuration.Tests;

/// <summary>
///     Tests for <see cref="KeyValueConfigurationWriter" />.
/// </summary>
public sealed class KeyValueConfigurationWriterTests
{
    /// <summary>
    ///     A snapshot round-trips through the parser/writer pair.
    /// </summary>
    [Test]
    public async Task Write_FromSnapshot_RoundTripsThroughParser()
    {
        var input = new Dictionary<string, string>
        {
            ["b.key"] = "two",
            ["a.key"] = "one",
            ["c.key"] = "three",
        };
        var snapshot = new ConfigurationSnapshot(input);

        var text = KeyValueConfigurationWriter.Write(snapshot);
        var parsed = KeyValueConfigurationParser.Parse(text);

        await Assert.That(parsed.IsSuccess).IsTrue();
        await Assert.That(parsed.Snapshot.Count).IsEqualTo(3);
        await Assert.That(parsed.Snapshot.Get("a.key", string.Empty)).IsEqualTo("one");
        await Assert.That(parsed.Snapshot.Get("b.key", string.Empty)).IsEqualTo("two");
        await Assert.That(parsed.Snapshot.Get("c.key", string.Empty)).IsEqualTo("three");
    }

    /// <summary>
    ///     Keys are emitted in alphabetical case-insensitive order.
    /// </summary>
    [Test]
    public async Task Write_FromDictionary_EmitsKeysInAlphabeticalOrder()
    {
        var input = new Dictionary<string, string>
        {
            ["zeta"] = "z",
            ["Alpha"] = "a",
            ["mike"] = "m",
        };

        var text = KeyValueConfigurationWriter.Write(input);

        var firstAlpha = text.IndexOf("Alpha=", System.StringComparison.OrdinalIgnoreCase);
        var firstMike = text.IndexOf("mike=", System.StringComparison.OrdinalIgnoreCase);
        var firstZeta = text.IndexOf("zeta=", System.StringComparison.OrdinalIgnoreCase);

        await Assert.That(firstAlpha).IsLessThan(firstMike);
        await Assert.That(firstMike).IsLessThan(firstZeta);
    }

    /// <summary>
    ///     Writing an empty dictionary yields an empty string.
    /// </summary>
    [Test]
    public async Task Write_EmptyDictionary_YieldsEmptyString()
    {
        var text = KeyValueConfigurationWriter.Write(new Dictionary<string, string>());

        await Assert.That(text).IsEqualTo(string.Empty);
    }

    /// <summary>
    ///     Writing an empty snapshot yields an empty string.
    /// </summary>
    [Test]
    public async Task Write_EmptySnapshot_YieldsEmptyString()
    {
        var text = KeyValueConfigurationWriter.Write(new ConfigurationSnapshot(new Dictionary<string, string>()));

        await Assert.That(text).IsEqualTo(string.Empty);
    }

    /// <summary>
    ///     Values containing spaces and equals signs round-trip through the parser.
    /// </summary>
    [Test]
    public async Task Write_ValuesWithSpacesAndEqualsSigns_RoundTripsThroughParser()
    {
        var input = new Dictionary<string, string>
        {
            ["query"] = "name = value",
            ["plain"] = "hello world",
        };

        var text = KeyValueConfigurationWriter.Write(input);
        var parsed = KeyValueConfigurationParser.Parse(text);

        await Assert.That(parsed.IsSuccess).IsTrue();
        await Assert.That(parsed.Snapshot.Get("query", string.Empty)).IsEqualTo("name = value");
        await Assert.That(parsed.Snapshot.Get("plain", string.Empty)).IsEqualTo("hello world");
    }
}
