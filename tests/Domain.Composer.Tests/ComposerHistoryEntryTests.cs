using System.Threading.Tasks;
using Proxyfan.Domain.Composer;
using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.Core;

namespace Proxyfan.Domain.Composer.Tests;

public sealed class ComposerHistoryEntryTests
{
    [Test]
    public async Task WithStarred_SettingTrue_ReturnsCopyWithStarFlagSet()
    {
        var builder = new ComposerRequestBuilder();
        builder.SetUrl("https://example.com/");
        var entry = new ComposerHistoryEntry(
            System.Guid.NewGuid(),
            builder.Build(),
            statusCode: 200,
            timestamp: System.DateTimeOffset.UtcNow,
            isStarred: false);

        var toggled = entry.WithStarred(true);

        await Assert.That(toggled.IsStarred).IsTrue();
        await Assert.That(toggled.Id).IsEqualTo(entry.Id);
        await Assert.That(toggled.Timestamp).IsEqualTo(entry.Timestamp);
        await Assert.That(toggled.StatusCode).IsEqualTo(entry.StatusCode);
        await Assert.That(entry.IsStarred).IsFalse();
    }
}
