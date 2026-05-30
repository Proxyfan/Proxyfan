using Proxyfan.Client.Inspector.ViewModels;
using Proxyfan.Domain.Traffic;
using System;
using System.Threading.Tasks;

namespace Proxyfan.Client.Tests;

/// <summary>
///     Tests for <see cref="ServerSentEventViewModel" />.
/// </summary>
public sealed class ServerSentEventViewModelTests
{
    /// <summary>
    ///     Verifies that all wrapper properties are derived from the supplied domain event.
    /// </summary>
    [Test]
    public async Task Constructor_FullyPopulatedEvent_DerivesAllText()
    {
        var domainEvent = new ServerSentEvent(
            "first line\nsecond line",
            eventType: "tick",
            id: "42",
            retryMilliseconds: 1500,
            timestamp: new DateTimeOffset(2026, 1, 2, 3, 4, 5, TimeSpan.Zero));

        var viewModel = new ServerSentEventViewModel(domainEvent);

        await Assert.That(viewModel.ServerSentEvent).IsSameReferenceAs(domainEvent);
        await Assert.That(viewModel.EventTypeText).IsEqualTo("tick");
        await Assert.That(viewModel.IdText).IsEqualTo("42");
        await Assert.That(viewModel.DataPreview).Contains("first line");
        await Assert.That(viewModel.SizeText).IsNotNull();
        await Assert.That(viewModel.TimestampText).IsEqualTo("03:04:05.000");
    }

    /// <summary>
    ///     Verifies that missing event type and id surface as friendly placeholder text.
    /// </summary>
    [Test]
    public async Task Constructor_OmittedEventTypeAndId_UsesPlaceholders()
    {
        var domainEvent = new ServerSentEvent("data", eventType: null, id: null, retryMilliseconds: null, timestamp: DateTimeOffset.UtcNow);

        var viewModel = new ServerSentEventViewModel(domainEvent);

        await Assert.That(viewModel.EventTypeText).IsEqualTo("(default)");
        await Assert.That(viewModel.IdText).IsEqualTo("(none)");
    }
}
