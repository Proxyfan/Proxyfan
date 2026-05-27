using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Proxyfan.Domain.Traffic.Tests;

/// <summary>
///     Tests for <see cref="ComposerHistoryService" />.
/// </summary>
public sealed class ComposerHistoryServiceTests
{
    /// <summary>
    ///     Verifies that adding an entry inserts it at the head of the history.
    /// </summary>
    [Test]
    public async Task Add_FirstEntry_InsertsAtHead()
    {
        var store = new InMemoryStore([]);
        var service = new ComposerHistoryService(store);

        service.Add(BuildEntry("https://example.com/a"));

        await Assert.That(service.Entries.Count).IsEqualTo(1);
        await Assert.That(service.Entries[0].Url).IsEqualTo("https://example.com/a");
    }

    /// <summary>
    ///     Verifies that the second entry is inserted at the head, pushing the first down.
    /// </summary>
    [Test]
    public async Task Add_SecondEntry_PutsItAtHead()
    {
        var store = new InMemoryStore([]);
        var service = new ComposerHistoryService(store);
        service.Add(BuildEntry("https://example.com/one"));

        service.Add(BuildEntry("https://example.com/two"));

        await Assert.That(service.Entries[0].Url).IsEqualTo("https://example.com/two");
        await Assert.That(service.Entries[1].Url).IsEqualTo("https://example.com/one");
    }

    /// <summary>
    ///     Verifies that the service evicts the oldest non-starred entry when the cap is exceeded.
    /// </summary>
    [Test]
    public async Task Add_ExceedingMaximum_EvictsOldestNonStarred()
    {
        var store = new InMemoryStore([]);
        var service = new ComposerHistoryService(store, maximumEntries: 2);
        service.Add(BuildEntry("https://example.com/one"));
        service.Add(BuildEntry("https://example.com/two"));

        service.Add(BuildEntry("https://example.com/three"));

        await Assert.That(service.Entries.Count).IsEqualTo(2);
        await Assert.That(service.Entries[0].Url).IsEqualTo("https://example.com/three");
        await Assert.That(service.Entries[1].Url).IsEqualTo("https://example.com/two");
    }

    /// <summary>
    ///     Verifies that starred entries survive eviction.
    /// </summary>
    [Test]
    public async Task Add_ExceedingMaximumWithStarred_KeepsStarred()
    {
        var store = new InMemoryStore([]);
        var service = new ComposerHistoryService(store, maximumEntries: 2);
        service.Add(BuildEntry("https://example.com/star", isStarred: true));
        service.Add(BuildEntry("https://example.com/one"));

        service.Add(BuildEntry("https://example.com/two"));

        await Assert.That(service.Entries.Count).IsEqualTo(2);
        await Assert.That(service.Entries[0].Url).IsEqualTo("https://example.com/two");
        await Assert.That(service.Entries[1].Url).IsEqualTo("https://example.com/star");
    }

    /// <summary>
    ///     Verifies the constructor rejects a maximum entries count less than one.
    /// </summary>
    [Test]
    public async Task Constructor_ZeroMaximum_ThrowsArgumentOutOfRange()
    {
        var store = new InMemoryStore([]);

        await Assert
            .That(() => new ComposerHistoryService(store, maximumEntries: 0))
            .Throws<ArgumentOutOfRangeException>();
    }

    /// <summary>
    ///     Verifies the constructor pre-loads entries from the store in the supplied order.
    /// </summary>
    [Test]
    public async Task Constructor_StorePopulated_LoadsEntries()
    {
        var entries = new List<ComposerHistoryEntry>
        {
            BuildEntry("https://example.com/first"),
            BuildEntry("https://example.com/second"),
        };
        var store = new InMemoryStore(entries);

        var service = new ComposerHistoryService(store);

        await Assert.That(service.Entries.Count).IsEqualTo(2);
        await Assert.That(service.Entries[0].Url).IsEqualTo("https://example.com/first");
    }

    /// <summary>
    ///     Verifies removing an unknown identifier returns false and does not persist.
    /// </summary>
    [Test]
    public async Task HasRemoved_UnknownId_ReturnsFalse()
    {
        var store = new InMemoryStore([]);
        var service = new ComposerHistoryService(store);
        service.Add(BuildEntry("https://example.com/a"));
        store.SaveCount = 0;

        var removed = service.HasRemoved(Guid.NewGuid());

        await Assert.That(removed).IsFalse();
        await Assert.That(store.SaveCount).IsEqualTo(0);
    }

    /// <summary>
    ///     Verifies that removing an existing entry deletes it and persists the updated list.
    /// </summary>
    [Test]
    public async Task HasRemoved_KnownId_RemovesAndPersists()
    {
        var store = new InMemoryStore([]);
        var service = new ComposerHistoryService(store);
        var entry = BuildEntry("https://example.com/a");
        service.Add(entry);
        store.SaveCount = 0;

        var removed = service.HasRemoved(entry.Id);

        await Assert.That(removed).IsTrue();
        await Assert.That(service.Entries.Count).IsEqualTo(0);
        await Assert.That(store.SaveCount).IsEqualTo(1);
    }

    /// <summary>
    ///     Verifies that <c>HasToggledStar</c> flips the flag on an existing entry.
    /// </summary>
    [Test]
    public async Task HasToggledStar_KnownId_FlipsFlag()
    {
        var store = new InMemoryStore([]);
        var service = new ComposerHistoryService(store);
        var entry = BuildEntry("https://example.com/a");
        service.Add(entry);

        service.HasToggledStar(entry.Id);

        await Assert.That(service.Entries[0].IsStarred).IsTrue();
    }

    /// <summary>
    ///     Verifies that <c>HasToggledStar</c> returns false for unknown identifiers.
    /// </summary>
    [Test]
    public async Task HasToggledStar_UnknownId_ReturnsFalse()
    {
        var store = new InMemoryStore([]);
        var service = new ComposerHistoryService(store);

        var toggled = service.HasToggledStar(Guid.NewGuid());

        await Assert.That(toggled).IsFalse();
    }

    /// <summary>
    ///     Verifies that <c>Search</c> with a blank query returns all entries.
    /// </summary>
    [Test]
    public async Task Search_BlankQuery_ReturnsAllEntries()
    {
        var store = new InMemoryStore([]);
        var service = new ComposerHistoryService(store);
        service.Add(BuildEntry("https://example.com/a"));
        service.Add(BuildEntry("https://example.com/b"));

        var results = service.Search("   ");

        await Assert.That(results.Count).IsEqualTo(2);
    }

    /// <summary>
    ///     Verifies that <c>Search</c> filters case-insensitively by URL substring.
    /// </summary>
    [Test]
    public async Task Search_Substring_FiltersCaseInsensitively()
    {
        var store = new InMemoryStore([]);
        var service = new ComposerHistoryService(store);
        service.Add(BuildEntry("https://example.com/users"));
        service.Add(BuildEntry("https://other.test/v1"));

        var results = service.Search("EXAMPLE");

        await Assert.That(results.Count).IsEqualTo(1);
        await Assert.That(results[0].Url).IsEqualTo("https://example.com/users");
    }

    private static ComposerHistoryEntry BuildEntry(string url, bool isStarred = false)
    {
        var headers = new Dictionary<string, string>(StringComparer.Ordinal);
        var entry = new ComposerHistoryEntry
        {
            Body = Encoding.UTF8.GetBytes("body"),
            Headers = headers,
            Id = Guid.NewGuid(),
            IsStarred = isStarred,
            Method = "GET",
            StatusCode = 200,
            Timestamp = DateTimeOffset.UtcNow,
            Url = url,
        };
        return entry;
    }

    private sealed class InMemoryStore : IComposerHistoryStore
    {
        private readonly IReadOnlyList<ComposerHistoryEntry> _initial;

        public int SaveCount { get; set; }

        public InMemoryStore(IReadOnlyList<ComposerHistoryEntry> initial)
        {
            _initial = initial;
        }

        public IReadOnlyList<ComposerHistoryEntry> Load()
        {
            return _initial;
        }

        public void Save(IReadOnlyList<ComposerHistoryEntry> entries)
        {
            SaveCount++;
        }
    }
}
