using System;
using System.Collections.Generic;
using System.Text.Json;

namespace Proxyfan.Presentation.Shortcuts;

/// <summary>
///     Serializes and deserializes the user's keyboard shortcut bindings as JSON. Format:
///     a top-level object with a <c>schemaVersion</c> field and a <c>bindings</c> array of
///     <c>{ action, key, modifiers }</c> entries. Unknown actions and unknown modifier flags
///     are skipped so that JSON written by a newer Proxyfan deserialises cleanly under an
///     older one.
/// </summary>
public static class ShortcutBindingsJsonSerializer
{
    /// <summary>
    ///     The current schema version embedded in the serialized JSON.
    /// </summary>
    public const int CurrentSchemaVersion = 1;
    private static readonly JsonSerializerOptions Options;

    static ShortcutBindingsJsonSerializer()
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true,
        };
        Options = options;
    }

    /// <summary>
    ///     Deserialises the supplied JSON text into a bindings map. Returns an empty map when
    ///     the JSON is empty, the schema version is unknown, or the payload is malformed.
    /// </summary>
    /// <param name="json">The JSON text to deserialise.</param>
    /// <returns>The deserialised bindings.</returns>
    public static IReadOnlyDictionary<ShortcutAction, KeyboardGesture> Deserialize(string json)
    {
        var empty = new Dictionary<ShortcutAction, KeyboardGesture>();

        if (string.IsNullOrWhiteSpace(json))
        {
            return empty;
        }

        ShortcutBindingsFile? file;
        try
        {
            file = JsonSerializer.Deserialize<ShortcutBindingsFile>(json, Options);
        }
        catch (JsonException)
        {
            return empty;
        }

        if (file is null || file.SchemaVersion != CurrentSchemaVersion || file.Bindings is null)
        {
            return empty;
        }

        var result = new Dictionary<ShortcutAction, KeyboardGesture>();

        foreach (var entry in file.Bindings)
        {
            ShortcutBindingsJsonHelpers.TryAddBinding(result, entry);
        }

        return result;
    }

    /// <summary>
    ///     Serialises the supplied bindings to JSON text with the current schema version.
    /// </summary>
    /// <param name="bindings">The bindings to serialise.</param>
    /// <returns>The JSON text.</returns>
    public static string Serialize(IReadOnlyDictionary<ShortcutAction, KeyboardGesture> bindings)
    {
        var entries = new List<RawShortcutBinding>();

        foreach (var binding in bindings)
        {
            var entry = new RawShortcutBinding
            {
                Action = binding.Key.ToString(),
                Key = binding.Value.Key,
                Modifiers = ShortcutBindingsJsonHelpers.ModifiersToStringArray(binding.Value.Modifiers),
            };
            entries.Add(entry);
        }

        var file = new ShortcutBindingsFile
        {
            Bindings = entries,
            SchemaVersion = CurrentSchemaVersion,
        };
        var json = JsonSerializer.Serialize(file, Options);
        return json;
    }

    private static class ShortcutBindingsJsonHelpers
    {
        public static List<string> ModifiersToStringArray(KeyboardModifier modifiers)
        {
            var list = new List<string>();

            if (modifiers.HasFlag(KeyboardModifier.Control))
            {
                list.Add("Control");
            }

            if (modifiers.HasFlag(KeyboardModifier.Shift))
            {
                list.Add("Shift");
            }

            if (modifiers.HasFlag(KeyboardModifier.Alt))
            {
                list.Add("Alt");
            }

            if (modifiers.HasFlag(KeyboardModifier.Meta))
            {
                list.Add("Meta");
            }

            return list;
        }

        public static KeyboardModifier ParseModifiers(IEnumerable<string>? modifiers)
        {
            var result = KeyboardModifier.None;

            if (modifiers is null)
            {
                return result;
            }

            foreach (var modifier in modifiers)
            {
                if (string.Equals(modifier, "Control", StringComparison.OrdinalIgnoreCase))
                {
                    result |= KeyboardModifier.Control;
                }
                else if (string.Equals(modifier, "Shift", StringComparison.OrdinalIgnoreCase))
                {
                    result |= KeyboardModifier.Shift;
                }
                else if (string.Equals(modifier, "Alt", StringComparison.OrdinalIgnoreCase))
                {
                    result |= KeyboardModifier.Alt;
                }
                else if (string.Equals(modifier, "Meta", StringComparison.OrdinalIgnoreCase))
                {
                    result |= KeyboardModifier.Meta;
                }
            }

            return result;
        }

        public static void TryAddBinding(
            Dictionary<ShortcutAction, KeyboardGesture> result,
            RawShortcutBinding entry)
        {
            if (string.IsNullOrWhiteSpace(entry.Action) || string.IsNullOrWhiteSpace(entry.Key))
            {
                return;
            }

            if (!Enum.TryParse<ShortcutAction>(entry.Action, ignoreCase: false, out var action))
            {
                return;
            }

            var gesture = new KeyboardGesture
            {
                Key = entry.Key,
                Modifiers = ParseModifiers(entry.Modifiers),
            };
            result[action] = gesture;
        }
    }

    private sealed class RawShortcutBinding
    {
        public string? Action { get; set; }

        public string? Key { get; set; }

        public List<string>? Modifiers { get; set; }
    }

    private sealed class ShortcutBindingsFile
    {
        public List<RawShortcutBinding>? Bindings { get; set; }

        public int SchemaVersion { get; set; }
    }
}
