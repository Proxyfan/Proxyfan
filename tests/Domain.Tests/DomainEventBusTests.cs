using System;
using System.Threading.Tasks;

namespace Proxyfan.Domain.Tests;

/// <summary>Tests for <see cref="DomainEventBus" />.</summary>
internal sealed class DomainEventBusTests
{
    private sealed record TestEvent(string Value) : IDomainEvent;
    private sealed record OtherEvent(int Number) : IDomainEvent;

    // ── Publish ───────────────────────────────────────────────────────────────

    /// <summary>Verifies that a registered handler is invoked when the matching event is published.</summary>
    [Test]
    public async Task Publish_WithRegisteredHandler_InvokesHandler()
    {
        var bus = new DomainEventBus();
        TestEvent? received = null;
        bus.Subscribe<TestEvent>(e => received = e);

        bus.Publish(new TestEvent("hello"));

        await Assert.That(received).IsNotNull();
        await Assert.That(received!.Value).IsEqualTo("hello");
    }

    /// <summary>Verifies that publishing to a bus with no subscribers does not throw.</summary>
    [Test]
    public async Task Publish_WithNoSubscribers_DoesNotThrow()
    {
        var bus = new DomainEventBus();
        await Assert.That(() => bus.Publish(new TestEvent("x"))).ThrowsNothing();
    }

    /// <summary>Verifies that all registered handlers for a type are invoked.</summary>
    [Test]
    public async Task Publish_WithMultipleHandlers_AllHandlersInvoked()
    {
        var bus = new DomainEventBus();
        var count = 0;
        bus.Subscribe<TestEvent>(_ => count++);
        bus.Subscribe<TestEvent>(_ => count++);
        bus.Subscribe<TestEvent>(_ => count++);

        bus.Publish(new TestEvent("x"));

        await Assert.That(count).IsEqualTo(3);
    }

    /// <summary>Verifies that a handler for one event type is not invoked for a different type.</summary>
    [Test]
    public async Task Publish_DifferentEventType_DoesNotInvokeWrongHandler()
    {
        var bus = new DomainEventBus();
        var invoked = false;
        bus.Subscribe<OtherEvent>(_ => invoked = true);

        bus.Publish(new TestEvent("x"));

        await Assert.That(invoked).IsFalse();
    }

    /// <summary>Verifies that a handler exception does not prevent subsequent handlers from executing.</summary>
    [Test]
    public async Task Publish_HandlerThrows_RemainingHandlersStillExecute()
    {
        var bus = new DomainEventBus();
        var secondInvoked = false;

        bus.Subscribe<TestEvent>(_ => throw new InvalidOperationException("boom"));
        bus.Subscribe<TestEvent>(_ => secondInvoked = true);

        bus.Publish(new TestEvent("x"));

        await Assert.That(secondInvoked).IsTrue();
    }

    // ── Subscribe / Unsubscribe ───────────────────────────────────────────────

    /// <summary>Verifies that disposing the subscription token removes the handler.</summary>
    [Test]
    public async Task Subscribe_WhenDisposed_HandlerIsNoLongerInvoked()
    {
        var bus = new DomainEventBus();
        var invoked = false;

        var subscription = bus.Subscribe<TestEvent>(_ => invoked = true);
        subscription.Dispose();
        bus.Publish(new TestEvent("x"));

        await Assert.That(invoked).IsFalse();
    }

    /// <summary>Verifies that disposing the same subscription multiple times does not throw.</summary>
    [Test]
    public async Task Subscribe_DisposedTwice_DoesNotThrow()
    {
        var bus = new DomainEventBus();
        var subscription = bus.Subscribe<TestEvent>(_ => { });

        subscription.Dispose();
        await Assert.That(subscription.Dispose).ThrowsNothing();
    }

    /// <summary>
    ///     Verifies that only the disposed handler is removed; other handlers for the same
    ///     type still execute.
    /// </summary>
    [Test]
    public async Task Subscribe_OneDisposed_OtherHandlersStillExecute()
    {
        var bus = new DomainEventBus();
        var count = 0;

        var sub1 = bus.Subscribe<TestEvent>(_ => count++);
        bus.Subscribe<TestEvent>(_ => count++);

        sub1.Dispose();
        bus.Publish(new TestEvent("x"));

        await Assert.That(count).IsEqualTo(1);
    }

    /// <summary>Verifies that the same handler can be registered multiple times and each registration fires independently.</summary>
    [Test]
    public async Task Subscribe_SameHandlerTwice_BothRegistrationsFire()
    {
        var bus = new DomainEventBus();
        var count = 0;

        void handler(TestEvent _)
        {
            count++;
        }

        bus.Subscribe<TestEvent>(handler);
        bus.Subscribe<TestEvent>(handler);
        bus.Publish(new TestEvent("x"));

        await Assert.That(count).IsEqualTo(2);
    }
}
