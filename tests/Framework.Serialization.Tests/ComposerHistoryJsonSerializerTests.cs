using Proxyfan.Domain.Traffic;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Proxyfan.Framework.Serialization.Tests;

/// <summary>
///     Tests for <see cref="ComposerHistoryJsonSerializer" />.
/// </summary>
public sealed class ComposerHistoryJsonSerializerTests
{
    /// <summary>
    ///     Verifies that <c>Deserialize</c> returns an empty list for whitespace JSON.
    /// </summary>
    [Test]
    public async Task Deserialize_BlankJson_ReturnsEmptyList()
    {
        var entries = ComposerHistoryJsonSerializer.Deserialize("   ");

        await Assert.That(entries.Count).IsEqualTo(0);
    }

    /// <summary>
    ///     Verifies that <c>Deserialize</c> returns an empty list when the JSON is not parseable.
    /// </summary>
    [Test]
    public async Task Deserialize_MalformedJson_ReturnsEmptyList()
    {
        var entries = ComposerHistoryJsonSerializer.Deserialize("{ not json");

        await Assert.That(entries.Count).IsEqualTo(0);
    }

    /// <summary>
    ///     Verifies that <c>Deserialize</c> returns an empty list for a future schema version.
    /// </summary>
    [Test]
    public async Task Deserialize_UnknownSchemaVersion_ReturnsEmptyList()
    {
        var json = "{ \"schemaVersion\": 999, \"entries\": [] }";

        var entries = ComposerHistoryJsonSerializer.Deserialize(json);

        await Assert.That(entries.Count).IsEqualTo(0);
    }

    /// <summary>
    ///     Verifies that <c>Serialize</c> followed by <c>Deserialize</c> roundtrips an entry.
    /// </summary>
    [Test]
    public async Task SerializeThenDeserialize_OneEntry_RoundtripsEntries()
    {
        var headers = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["X-Custom"] = "value",
        };
        var entry = new ComposerHistoryEntry
        {
            Body = Encoding.UTF8.GetBytes("hello"),
            Headers = headers,
            Id = Guid.NewGuid(),
            IsStarred = true,
            Method = "POST",
            StatusCode = 201,
            Timestamp = new DateTimeOffset(2024, 5, 6, 7, 8, 9, TimeSpan.Zero),
            Url = "https://example.com/api",
        };
        var entries = new List<ComposerHistoryEntry> { entry };

        var json = ComposerHistoryJsonSerializer.Serialize(entries);
        var roundtripped = ComposerHistoryJsonSerializer.Deserialize(json);

        await Assert.That(roundtripped.Count).IsEqualTo(1);
        var first = roundtripped[0];
        await Assert.That(first.Url).IsEqualTo("https://example.com/api");
        await Assert.That(first.Method).IsEqualTo("POST");
        await Assert.That(first.IsStarred).IsTrue();
        await Assert.That(first.StatusCode).IsEqualTo(201);
        await Assert.That(first.Headers["X-Custom"]).IsEqualTo("value");
        await Assert.That(Encoding.UTF8.GetString(first.Body.Span)).IsEqualTo("hello");
    }

    /// <summary>
    ///     Verifies that <c>WriteToFile</c> and <c>ReadFromFile</c> roundtrip via the file system.
    /// </summary>
    [Test]
    public async Task WriteThenRead_OneEntry_RoundtripsViaFileSystem()
    {
        var temp = Path.Combine(Path.GetTempPath(), "Proxyfan-tests", Guid.NewGuid().ToString("N"), "history.json");
        try
        {
            var headers = new Dictionary<string, string>(StringComparer.Ordinal);
            var entry = new ComposerHistoryEntry
            {
                Body = Array.Empty<byte>(),
                Headers = headers,
                Id = Guid.NewGuid(),
                IsStarred = false,
                Method = "GET",
                StatusCode = 200,
                Timestamp = DateTimeOffset.UtcNow,
                Url = "https://example.com/",
            };
            var entries = new List<ComposerHistoryEntry> { entry };

            ComposerHistoryJsonSerializer.WriteToFile(temp, entries);
            var roundtripped = ComposerHistoryJsonSerializer.ReadFromFile(temp);

            await Assert.That(roundtripped.Count).IsEqualTo(1);
            await Assert.That(roundtripped[0].Url).IsEqualTo("https://example.com/");
        }
        finally
        {
            var directory = Path.GetDirectoryName(temp);

            if (directory is not null && Directory.Exists(directory))
            {
                Directory.Delete(directory, recursive: true);
            }
        }
    }

    /// <summary>
    ///     Verifies that <c>ReadFromFile</c> returns an empty list when the file does not exist.
    /// </summary>
    [Test]
    public async Task ReadFromFile_MissingFile_ReturnsEmptyList()
    {
        var nonexistent = Path.Combine(Path.GetTempPath(), "Proxyfan-tests", Guid.NewGuid().ToString("N"), "missing.json");

        var entries = ComposerHistoryJsonSerializer.ReadFromFile(nonexistent);

        await Assert.That(entries.Count).IsEqualTo(0);
    }

    /// <summary>
    ///     An entry missing the Method field is dropped during deserialization
    ///     (covers the null-method branch of TryConvert).
    /// </summary>
    [Test]
    public async Task Deserialize_EntryMissingMethod_SkipsEntry()
    {
        var json = "{\"schemaVersion\":1,\"entries\":[{\"url\":\"https://example.com/api\",\"version\":\"HTTP/1.1\"}]}";

        var entries = ComposerHistoryJsonSerializer.Deserialize(json);

        await Assert.That(entries.Count).IsEqualTo(0);
    }

    /// <summary>
    ///     An entry missing the Url field is dropped during deserialization
    ///     (covers the null-url branch of TryConvert).
    /// </summary>
    [Test]
    public async Task Deserialize_EntryMissingUrl_SkipsEntry()
    {
        var json = "{\"schemaVersion\":1,\"entries\":[{\"method\":\"GET\",\"version\":\"HTTP/1.1\"}]}";

        var entries = ComposerHistoryJsonSerializer.Deserialize(json);

        await Assert.That(entries.Count).IsEqualTo(0);
    }

    /// <summary>
    ///     An entry with invalid base64 body data is dropped during deserialization
    ///     (covers the FormatException catch branch of TryConvert).
    /// </summary>
    [Test]
    public async Task Deserialize_EntryWithInvalidBase64_SkipsEntry()
    {
        var json = "{\"schemaVersion\":1,\"entries\":[{\"method\":\"POST\",\"url\":\"https://example.com/api\",\"version\":\"HTTP/1.1\",\"bodyBase64\":\"@@@not-base64@@@\"}]}";

        var entries = ComposerHistoryJsonSerializer.Deserialize(json);

        await Assert.That(entries.Count).IsEqualTo(0);
    }

    /// <summary>
    ///     An entry with a null headers dictionary is parsed using an empty header
    ///     collection (covers the null headers branch in TryConvert).
    /// </summary>
    [Test]
    public async Task Deserialize_EntryWithNullHeaders_UsesEmptyHeaders()
    {
        var json = "{\"schemaVersion\":1,\"entries\":[{\"method\":\"GET\",\"url\":\"https://example.com/api\",\"version\":\"HTTP/1.1\",\"savedAt\":\"2024-01-01T00:00:00Z\"}]}";

        var entries = ComposerHistoryJsonSerializer.Deserialize(json);

        await Assert.That(entries.Count).IsEqualTo(1);
        await Assert.That(entries[0].Headers.Count).IsEqualTo(0);
    }
}
