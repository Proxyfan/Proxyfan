using System;
using System.Threading.Tasks;
using Proxyfan.Domain.Composer;
using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.Core;

namespace Proxyfan.Domain.Composer.Tests;

public sealed class ComposerHistoryServiceTests
{
    [Test]
    public async Task Add_NewEntry_AppearsAtIndexZero()
    {
        var service = new ComposerHistoryService();
        var entry = MakeEntry("https://first.example/");

        service.Add(entry);

        var snapshot = service.Snapshot();
        await Assert.That(snapshot.Count).IsEqualTo(1);
        await Assert.That(snapshot[0].Id).IsEqualTo(entry.Id);
    }

    [Test]
    public async Task Select_LinqRemoval_ReplacedByForLoop()
    {
        var service = new ComposerHistoryService();
        var first = MakeEntry("https://a.example/");
        var second = MakeEntry("https://b.example/");
        var third = MakeEntry("https://c.example/");
        service.Add(first);
        service.Add(second);
        service.Add(third);

        var snapshot = service.Snapshot();

        await Assert.That(snapshot.Count).IsEqualTo(3);
        await Assert.That(snapshot[0].Request.Url).IsEqualTo("https://c.example/");
        await Assert.That(snapshot[1].Request.Url).IsEqualTo("https://b.example/");
        await Assert.That(snapshot[2].Request.Url).IsEqualTo("https://a.example/");
    }

    [Test]
    public async Task Add_ExceedingCapacity_EvictsOldestUnstarred()
    {
        var service = new ComposerHistoryService(capacity: 2);
        service.Add(MakeEntry("https://a.example/"));
        service.Add(MakeEntry("https://b.example/"));
        service.Add(MakeEntry("https://c.example/"));

        var snapshot = service.Snapshot();

        await Assert.That(snapshot.Count).IsEqualTo(2);
        await Assert.That(snapshot[0].Request.Url).IsEqualTo("https://c.example/");
        await Assert.That(snapshot[1].Request.Url).IsEqualTo("https://b.example/");
    }

    [Test]
    public async Task Add_ExceedingCapacityWithStarredOldest_EvictsNextUnstarred()
    {
        var service = new ComposerHistoryService(capacity: 2);
        var starred = MakeEntry("https://starred.example/", isStarred: true);
        service.Add(starred);
        service.Add(MakeEntry("https://middle.example/"));
        service.Add(MakeEntry("https://newest.example/"));

        var snapshot = service.Snapshot();

        await Assert.That(snapshot.Count).IsEqualTo(2);
        var hasStarred = false;
        for (var index = 0; index < snapshot.Count; index++)
        {
            if (snapshot[index].Id == starred.Id)
            {
                hasStarred = true;
            }
        }
        await Assert.That(hasStarred).IsTrue();
        await Assert.That(snapshot[0].Request.Url).IsEqualTo("https://newest.example/");
    }

    [Test]
    public async Task Add_ExceedingCapacityWhenAllStarred_EvictsOldestStarred()
    {
        var service = new ComposerHistoryService(capacity: 2);
        var oldStarred = MakeEntry("https://old.example/", isStarred: true);
        var midStarred = MakeEntry("https://mid.example/", isStarred: true);
        var newStarred = MakeEntry("https://new.example/", isStarred: true);
        service.Add(oldStarred);
        service.Add(midStarred);
        service.Add(newStarred);

        var snapshot = service.Snapshot();

        await Assert.That(snapshot.Count).IsEqualTo(2);
        var stillContainsOldest = false;
        for (var index = 0; index < snapshot.Count; index++)
        {
            if (snapshot[index].Id == oldStarred.Id)
            {
                stillContainsOldest = true;
            }
        }
        await Assert.That(stillContainsOldest).IsFalse();
    }

    [Test]
    public async Task HasToggledStar_KnownEntry_TogglesIsStarred()
    {
        var service = new ComposerHistoryService();
        var entry = MakeEntry("https://example.com/", isStarred: false);
        service.Add(entry);

        var toggled = service.HasToggledStar(entry.Id);

        await Assert.That(toggled).IsTrue();
        await Assert.That(service.Snapshot()[0].IsStarred).IsTrue();
    }

    [Test]
    public async Task HasToggledStar_UnknownEntry_ReturnsFalse()
    {
        var service = new ComposerHistoryService();

        var toggled = service.HasToggledStar(Guid.NewGuid());

        await Assert.That(toggled).IsFalse();
    }

    [Test]
    public async Task Clear_PopulatedHistory_RemovesAllEntries()
    {
        var service = new ComposerHistoryService();
        service.Add(MakeEntry("https://a.example/"));
        service.Add(MakeEntry("https://b.example/", isStarred: true));

        service.Clear();

        await Assert.That(service.Snapshot().Count).IsEqualTo(0);
    }

    [Test]
    public async Task Search_CaseInsensitiveSubstring_MatchesUrl()
    {
        var service = new ComposerHistoryService();
        service.Add(MakeEntry("https://api.github.com/users"));
        service.Add(MakeEntry("https://github.com/repos"));
        service.Add(MakeEntry("https://example.com/"));

        var matches = service.Search("GITHUB");

        await Assert.That(matches.Count).IsEqualTo(2);
    }

    [Test]
    public async Task Search_NoMatch_ReturnsEmpty()
    {
        var service = new ComposerHistoryService();
        service.Add(MakeEntry("https://example.com/"));

        var matches = service.Search("missing-term");

        await Assert.That(matches.Count).IsEqualTo(0);
    }

    [Test]
    public async Task Constructor_NonPositiveCapacity_Throws()
    {
        await Assert.That(() => new ComposerHistoryService(capacity: 0))
            .Throws<ArgumentOutOfRangeException>();
    }

    private static ComposerHistoryEntry MakeEntry(string url, bool isStarred = false)
    {
        var builder = new ComposerRequestBuilder();
        builder.SetUrl(url);
        var request = builder.Build();
        var entry = new ComposerHistoryEntry(
            Guid.NewGuid(),
            request,
            statusCode: 200,
            timestamp: DateTimeOffset.UtcNow,
            isStarred: isStarred);
        return entry;
    }
}
