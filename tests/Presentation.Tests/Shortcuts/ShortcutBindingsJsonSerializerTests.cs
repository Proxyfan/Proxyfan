using System.Collections.Generic;
using System.Threading.Tasks;
using Proxyfan.Presentation.Shortcuts;

namespace Proxyfan.Presentation.Tests.Shortcuts;

/// <summary>
///     Tests for <see cref="ShortcutBindingsJsonSerializer" />.
/// </summary>
public sealed class ShortcutBindingsJsonSerializerTests
{
    /// <summary>
    ///     Verifies a full round-trip preserves every binding.
    /// </summary>
    [Test]
    public async Task SerializeDeserialize_RoundTrip_PreservesAllBindings()
    {
        var input = new Dictionary<ShortcutAction, KeyboardGesture>
        {
            [ShortcutAction.ToggleCapture] = new() { Key = "R", Modifiers = KeyboardModifier.Control },
            [ShortcutAction.Find] = new() { Key = "F", Modifiers = KeyboardModifier.Control },
            [ShortcutAction.ToggleNoCaching] = new() { Key = "N", Modifiers = KeyboardModifier.Control | KeyboardModifier.Shift },
        };

        var json = ShortcutBindingsJsonSerializer.Serialize(input);
        var output = ShortcutBindingsJsonSerializer.Deserialize(json);

        await Assert.That(output.Count).IsEqualTo(3);
        await Assert.That(output[ShortcutAction.ToggleCapture].Key).IsEqualTo("R");
        await Assert.That(output[ShortcutAction.ToggleCapture].Modifiers).IsEqualTo(KeyboardModifier.Control);
        await Assert.That(output[ShortcutAction.Find].Key).IsEqualTo("F");
        await Assert.That(output[ShortcutAction.ToggleNoCaching].Modifiers)
            .IsEqualTo(KeyboardModifier.Control | KeyboardModifier.Shift);
    }

    /// <summary>
    ///     Verifies an empty JSON string deserialises to an empty map.
    /// </summary>
    [Test]
    public async Task Deserialize_EmptyString_ReturnsEmpty()
    {
        var result = ShortcutBindingsJsonSerializer.Deserialize(string.Empty);

        await Assert.That(result.Count).IsEqualTo(0);
    }

    /// <summary>
    ///     Verifies whitespace-only JSON deserialises to an empty map.
    /// </summary>
    [Test]
    public async Task Deserialize_Whitespace_ReturnsEmpty()
    {
        var result = ShortcutBindingsJsonSerializer.Deserialize("   \n\t  ");

        await Assert.That(result.Count).IsEqualTo(0);
    }

    /// <summary>
    ///     Verifies malformed JSON deserialises to an empty map (no throw).
    /// </summary>
    [Test]
    public async Task Deserialize_MalformedJson_ReturnsEmpty()
    {
        var result = ShortcutBindingsJsonSerializer.Deserialize("{ not valid json");

        await Assert.That(result.Count).IsEqualTo(0);
    }

    /// <summary>
    ///     Verifies a wrong schema version is rejected and yields an empty map.
    /// </summary>
    [Test]
    public async Task Deserialize_WrongSchemaVersion_ReturnsEmpty()
    {
        const string json = """{ "schemaVersion": 99, "bindings": [{ "action": "Find", "key": "F", "modifiers": ["Control"] }] }""";

        var result = ShortcutBindingsJsonSerializer.Deserialize(json);

        await Assert.That(result.Count).IsEqualTo(0);
    }

    /// <summary>
    ///     Verifies unknown action names are silently skipped.
    /// </summary>
    [Test]
    public async Task Deserialize_UnknownAction_SkipsEntry()
    {
        const string json = """
            {
              "schemaVersion": 1,
              "bindings": [
                { "action": "NotARealAction", "key": "X", "modifiers": ["Control"] },
                { "action": "Find", "key": "F", "modifiers": ["Control"] }
              ]
            }
            """;

        var result = ShortcutBindingsJsonSerializer.Deserialize(json);

        await Assert.That(result.Count).IsEqualTo(1);
        await Assert.That(result.ContainsKey(ShortcutAction.Find)).IsTrue();
    }

    /// <summary>
    ///     Verifies unknown modifier names are silently skipped while known ones survive.
    /// </summary>
    [Test]
    public async Task Deserialize_UnknownModifier_SkipsModifierKeepsKnown()
    {
        const string json = """
            {
              "schemaVersion": 1,
              "bindings": [
                { "action": "Find", "key": "F", "modifiers": ["Control", "BogusKey", "Shift"] }
              ]
            }
            """;

        var result = ShortcutBindingsJsonSerializer.Deserialize(json);

        await Assert.That(result.Count).IsEqualTo(1);
        await Assert.That(result[ShortcutAction.Find].Modifiers)
            .IsEqualTo(KeyboardModifier.Control | KeyboardModifier.Shift);
    }

    /// <summary>
    ///     Verifies entries missing required fields are skipped.
    /// </summary>
    [Test]
    public async Task Deserialize_MissingActionOrKey_SkipsEntry()
    {
        const string json = """
            {
              "schemaVersion": 1,
              "bindings": [
                { "key": "F", "modifiers": ["Control"] },
                { "action": "Find", "modifiers": ["Control"] },
                { "action": "ClearTraffic", "key": "K", "modifiers": ["Control"] }
              ]
            }
            """;

        var result = ShortcutBindingsJsonSerializer.Deserialize(json);

        await Assert.That(result.Count).IsEqualTo(1);
        await Assert.That(result.ContainsKey(ShortcutAction.ClearTraffic)).IsTrue();
    }

    /// <summary>
    ///     Verifies all four modifier flags survive a round-trip.
    /// </summary>
    [Test]
    public async Task SerializeDeserialize_AllFourModifiers_PreservesAllFlags()
    {
        var input = new Dictionary<ShortcutAction, KeyboardGesture>
        {
            [ShortcutAction.Find] = new()
            {
                Key = "F",
                Modifiers = KeyboardModifier.Control | KeyboardModifier.Shift | KeyboardModifier.Alt | KeyboardModifier.Meta,
            },
        };

        var json = ShortcutBindingsJsonSerializer.Serialize(input);
        var output = ShortcutBindingsJsonSerializer.Deserialize(json);

        var combined = KeyboardModifier.Control | KeyboardModifier.Shift | KeyboardModifier.Alt | KeyboardModifier.Meta;
        await Assert.That(output[ShortcutAction.Find].Modifiers).IsEqualTo(combined);
    }

    /// <summary>
    ///     Verifies an empty bindings map serialises and deserialises cleanly.
    /// </summary>
    [Test]
    public async Task SerializeDeserialize_EmptyMap_RoundTripsToEmpty()
    {
        var input = new Dictionary<ShortcutAction, KeyboardGesture>();

        var json = ShortcutBindingsJsonSerializer.Serialize(input);
        var output = ShortcutBindingsJsonSerializer.Deserialize(json);

        await Assert.That(output.Count).IsEqualTo(0);
    }

    /// <summary>
    ///     Verifies an empty bindings array deserialises to an empty map.
    /// </summary>
    [Test]
    public async Task Deserialize_EmptyBindingsArray_ReturnsEmpty()
    {
        const string json = """{ "schemaVersion": 1, "bindings": [] }""";

        var result = ShortcutBindingsJsonSerializer.Deserialize(json);

        await Assert.That(result.Count).IsEqualTo(0);
    }

    /// <summary>
    ///     Verifies missing bindings property is treated as empty.
    /// </summary>
    [Test]
    public async Task Deserialize_MissingBindings_ReturnsEmpty()
    {
        const string json = """{ "schemaVersion": 1 }""";

        var result = ShortcutBindingsJsonSerializer.Deserialize(json);

        await Assert.That(result.Count).IsEqualTo(0);
    }

    /// <summary>
    ///     Verifies modifier name matching is case-insensitive.
    /// </summary>
    [Test]
    public async Task Deserialize_LowerCaseModifiers_ParsesAllFlags()
    {
        const string json = """
            {
              "schemaVersion": 1,
              "bindings": [
                { "action": "Find", "key": "F", "modifiers": ["control", "shift"] }
              ]
            }
            """;

        var result = ShortcutBindingsJsonSerializer.Deserialize(json);

        await Assert.That(result[ShortcutAction.Find].Modifiers)
            .IsEqualTo(KeyboardModifier.Control | KeyboardModifier.Shift);
    }
}
