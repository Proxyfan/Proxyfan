using Proxyfan.Domain.Traffic;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace Proxyfan.Framework.Serialization;

/// <summary>
///     Serializes <see cref="ComposerHistoryEntry" /> collections to and from a JSON file on
///     disk. The file format carries a <c>schemaVersion</c> field so future versions can detect
///     and upgrade older formats. Used by the Request Composer tool to persist the user's
///     compose-and-send history across application launches.
/// </summary>
public static class ComposerHistoryJsonSerializer
{
    /// <summary>
    ///     The current schema version embedded in the serialized JSON.
    /// </summary>
    public const int CurrentSchemaVersion = 1;
    private static readonly JsonSerializerOptions Options;

    static ComposerHistoryJsonSerializer()
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true,
        };
        Options = options;
    }

    /// <summary>
    ///     Deserializes a list of <see cref="ComposerHistoryEntry" /> values from JSON text.
    ///     Returns an empty list when the JSON is empty, the schema version is unknown, or any
    ///     entry is malformed.
    /// </summary>
    /// <param name="json">The JSON text to deserialize.</param>
    /// <returns>The deserialized entries.</returns>
    public static IReadOnlyList<ComposerHistoryEntry> Deserialize(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return [];
        }

        ComposerHistoryFile? file;
        try
        {
            file = JsonSerializer.Deserialize<ComposerHistoryFile>(json, Options);
        }
        catch (JsonException)
        {
            return [];
        }

        if (file is null || file.SchemaVersion != CurrentSchemaVersion || file.Entries is null)
        {
            return [];
        }

        var entries = new List<ComposerHistoryEntry>(file.Entries.Count);

        foreach (var raw in file.Entries)
        {
            var entry = TryConvert(raw);

            if (entry is not null)
            {
                entries.Add(entry);
            }
        }

        return entries;
    }

    /// <summary>
    ///     Reads and deserializes Composer history from the supplied file path. Returns an empty
    ///     list when the file does not exist.
    /// </summary>
    /// <param name="filePath">The absolute path to the history file.</param>
    /// <returns>The deserialized entries.</returns>
    public static IReadOnlyList<ComposerHistoryEntry> ReadFromFile(string filePath)
    {
        if (!File.Exists(filePath))
        {
            return [];
        }

        var json = File.ReadAllText(filePath);
        return Deserialize(json);
    }

    /// <summary>
    ///     Serializes the supplied entries to JSON text with the current schema version.
    /// </summary>
    /// <param name="entries">The entries to serialize.</param>
    /// <returns>The JSON text.</returns>
    public static string Serialize(IReadOnlyList<ComposerHistoryEntry> entries)
    {
        var rawEntries = new List<RawComposerHistoryEntry>(entries.Count);

        foreach (var entry in entries)
        {
            var headers = new Dictionary<string, string>(entry.Headers.Count, StringComparer.Ordinal);

            foreach (var header in entry.Headers)
            {
                headers[header.Key] = header.Value;
            }

            var raw = new RawComposerHistoryEntry
            {
                BodyBase64 = Convert.ToBase64String(entry.Body.Span),
                Headers = headers,
                Id = entry.Id,
                IsStarred = entry.IsStarred,
                Method = entry.Method,
                StatusCode = entry.StatusCode,
                Timestamp = entry.Timestamp,
                Url = entry.Url,
            };
            rawEntries.Add(raw);
        }

        var file = new ComposerHistoryFile
        {
            Entries = rawEntries,
            SchemaVersion = CurrentSchemaVersion,
        };
        var json = JsonSerializer.Serialize(file, Options);
        return json;
    }

    /// <summary>
    ///     Serializes the supplied entries to JSON and writes them to the supplied file path,
    ///     creating any missing parent directories.
    /// </summary>
    /// <param name="filePath">The absolute path to write to.</param>
    /// <param name="entries">The entries to write.</param>
    public static void WriteToFile(string filePath, IReadOnlyList<ComposerHistoryEntry> entries)
    {
        var directory = Path.GetDirectoryName(filePath);

        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var json = Serialize(entries);
        File.WriteAllText(filePath, json);
    }

    private static ComposerHistoryEntry? TryConvert(RawComposerHistoryEntry raw)
    {
        if (raw.Method is null || raw.Url is null)
        {
            return null;
        }

        byte[] bodyBytes;
        try
        {
            bodyBytes = string.IsNullOrEmpty(raw.BodyBase64)
                ? []
                : Convert.FromBase64String(raw.BodyBase64);
        }
        catch (FormatException)
        {
            return null;
        }

        Dictionary<string, string> headers;

        if (raw.Headers is null)
        {
            var empty = new Dictionary<string, string>(StringComparer.Ordinal);
            headers = empty;
        }
        else
        {
            headers = raw.Headers;
        }

        var bodyMemory = new ReadOnlyMemory<byte>(bodyBytes);
        var entry = new ComposerHistoryEntry
        {
            Body = bodyMemory,
            Headers = headers,
            Id = raw.Id,
            IsStarred = raw.IsStarred,
            Method = raw.Method,
            StatusCode = raw.StatusCode,
            Timestamp = raw.Timestamp,
            Url = raw.Url,
        };
        return entry;
    }

    private sealed class ComposerHistoryFile
    {
        public List<RawComposerHistoryEntry>? Entries { get; set; }

        public int SchemaVersion { get; set; }
    }

    private sealed class RawComposerHistoryEntry
    {
        public string? BodyBase64 { get; set; }

        public Dictionary<string, string>? Headers { get; set; }

        public Guid Id { get; set; }

        public bool IsStarred { get; set; }

        public string? Method { get; set; }

        public int? StatusCode { get; set; }

        public DateTimeOffset Timestamp { get; set; }

        public string? Url { get; set; }
    }
}
