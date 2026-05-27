using Proxyfan.Domain.Traffic;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Proxyfan.Framework.Serialization.Tests;

/// <summary>
///     Tests for <see cref="FileComposerHistoryStore" />.
/// </summary>
public sealed class FileComposerHistoryStoreTests
{
    /// <summary>
    ///     Verifies that <see cref="FileComposerHistoryStore.Load" /> returns an empty list when
    ///     the backing file does not exist.
    /// </summary>
    [Test]
    public async Task Load_MissingFile_ReturnsEmpty()
    {
        var path = Path.Combine(Path.GetTempPath(), "proxyfan-tests", Guid.NewGuid().ToString("N"), "history.json");
        var store = new FileComposerHistoryStore(path);

        var entries = store.Load();

        await Assert.That(entries.Count).IsEqualTo(0);
    }

    /// <summary>
    ///     Verifies that saving and loading round-trips entries through the JSON file.
    /// </summary>
    [Test]
    public async Task SaveLoad_SingleEntry_RoundTrips()
    {
        var directory = Path.Combine(Path.GetTempPath(), "proxyfan-tests", Guid.NewGuid().ToString("N"));
        var path = Path.Combine(directory, "history.json");
        try
        {
            var store = new FileComposerHistoryStore(path);
            var headers = new Dictionary<string, string>(StringComparer.Ordinal) { ["Accept"] = "text/plain" };
            var entry = new ComposerHistoryEntry
            {
                Body = Encoding.UTF8.GetBytes("hello"),
                Headers = headers,
                Id = Guid.NewGuid(),
                IsStarred = true,
                Method = "GET",
                StatusCode = 200,
                Timestamp = DateTimeOffset.UtcNow,
                Url = "https://example.com/",
            };
            store.Save(new List<ComposerHistoryEntry> { entry });

            var loaded = store.Load();

            await Assert.That(loaded.Count).IsEqualTo(1);
            await Assert.That(loaded[0].Id).IsEqualTo(entry.Id);
            await Assert.That(loaded[0].Method).IsEqualTo("GET");
            await Assert.That(loaded[0].IsStarred).IsTrue();
        }
        finally
        {
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, recursive: true);
            }
        }
    }
}
