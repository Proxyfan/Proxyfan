using Proxyfan.Client.Tools.ViewModels;
using Proxyfan.Domain.Traffic;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.Core;

namespace Proxyfan.Client.Tests;

/// <summary>
///     Tests for <see cref="ComposerHistoryEntryViewModel" />.
/// </summary>
public sealed class ComposerHistoryEntryViewModelTests
{
    [Test]
    public async Task Constructor_FromEntry_ExposesSourceProperties()
    {
        var entry = new ComposerHistoryEntry
        {
            Body = Array.Empty<byte>(),
            Headers = new Dictionary<string, string>(),
            Id = Guid.NewGuid(),
            IsStarred = true,
            Method = "POST",
            StatusCode = 200,
            Timestamp = DateTimeOffset.UtcNow,
            Url = "https://example.com/api",
        };

        var viewModel = new ComposerHistoryEntryViewModel(entry);

        await Assert.That(viewModel.Source).IsSameReferenceAs(entry);
        await Assert.That(viewModel.Method).IsEqualTo("POST");
        await Assert.That(viewModel.Url).IsEqualTo("https://example.com/api");
        await Assert.That(viewModel.IsStarred).IsTrue();
    }

    [Test]
    public async Task Constructor_UnstarredEntry_PropagatesFlag()
    {
        var entry = new ComposerHistoryEntry
        {
            Body = Array.Empty<byte>(),
            Headers = new Dictionary<string, string>(),
            Id = Guid.NewGuid(),
            IsStarred = false,
            Method = "GET",
            StatusCode = null,
            Timestamp = DateTimeOffset.UtcNow,
            Url = "https://example.com/",
        };

        var viewModel = new ComposerHistoryEntryViewModel(entry);

        await Assert.That(viewModel.IsStarred).IsFalse();
    }
}
