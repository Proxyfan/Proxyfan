using Proxyfan.Domain.Rules.Pipeline;
using Proxyfan.Domain.Traffic;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Proxyfan.Domain.Rules.Tests;

/// <summary>
///     Tests for <see cref="RuleRegistry" />.
/// </summary>
public sealed class RuleRegistryTests
{
    /// <summary>
    ///     A freshly constructed registry exposes empty request- and response-phase snapshots.
    /// </summary>
    [Test]
    public async Task GetRequestPhaseRules_NewRegistry_IsEmpty()
    {
        var registry = new RuleRegistry();

        await Assert.That(registry.GetRequestPhaseRules().Count).IsEqualTo(0);
        await Assert.That(registry.GetResponsePhaseRules().Count).IsEqualTo(0);
    }

    /// <summary>
    ///     Request-phase rules are returned sorted ascending by <see cref="IRule.Priority" />.
    /// </summary>
    [Test]
    public async Task GetRequestPhaseRules_AfterRegistration_ReturnsSortedSnapshot()
    {
        var registry = new RuleRegistry();
        var ruleHigh = new StubRequestRule(priority: 10);
        var ruleLow = new StubRequestRule(priority: 1);
        var ruleMid = new StubRequestRule(priority: 5);

        registry.RegisterRequestPhaseRule(ruleHigh);
        registry.RegisterRequestPhaseRule(ruleLow);
        registry.RegisterRequestPhaseRule(ruleMid);

        var snapshot = registry.GetRequestPhaseRules();

        await Assert.That(snapshot.Count).IsEqualTo(3);
        await Assert.That(snapshot[0]).IsSameReferenceAs(ruleLow);
        await Assert.That(snapshot[1]).IsSameReferenceAs(ruleMid);
        await Assert.That(snapshot[2]).IsSameReferenceAs(ruleHigh);
    }

    /// <summary>
    ///     Response-phase rules are returned sorted ascending by <see cref="IRule.Priority" />.
    /// </summary>
    [Test]
    public async Task GetResponsePhaseRules_AfterRegistration_ReturnsSortedSnapshot()
    {
        var registry = new RuleRegistry();
        var ruleHigh = new StubResponseRule(priority: 10);
        var ruleLow = new StubResponseRule(priority: 1);

        registry.RegisterResponsePhaseRule(ruleHigh);
        registry.RegisterResponsePhaseRule(ruleLow);

        var snapshot = registry.GetResponsePhaseRules();

        await Assert.That(snapshot.Count).IsEqualTo(2);
        await Assert.That(snapshot[0]).IsSameReferenceAs(ruleLow);
        await Assert.That(snapshot[1]).IsSameReferenceAs(ruleHigh);
    }

    /// <summary>
    ///     Each snapshot is a defensive copy: mutating the registry afterwards does not affect prior snapshots.
    /// </summary>
    [Test]
    public async Task GetRequestPhaseRules_SnapshotIsDefensive_NotAffectedByLaterRegistrations()
    {
        var registry = new RuleRegistry();
        registry.RegisterRequestPhaseRule(new StubRequestRule(priority: 1));

        var firstSnapshot = registry.GetRequestPhaseRules();

        registry.RegisterRequestPhaseRule(new StubRequestRule(priority: 2));

        await Assert.That(firstSnapshot.Count).IsEqualTo(1);
        await Assert.That(registry.GetRequestPhaseRules().Count).IsEqualTo(2);
    }

    /// <summary>
    ///     Unregistering a previously registered request-phase rule removes it from the snapshot.
    /// </summary>
    [Test]
    public async Task UnregisterRequestPhaseRule_RegisteredRule_RemovesFromSnapshot()
    {
        var registry = new RuleRegistry();
        var rule = new StubRequestRule(priority: 1);
        registry.RegisterRequestPhaseRule(rule);

        registry.UnregisterRequestPhaseRule(rule);

        await Assert.That(registry.GetRequestPhaseRules().Count).IsEqualTo(0);
    }

    /// <summary>
    ///     Unregistering a previously registered response-phase rule removes it from the snapshot.
    /// </summary>
    [Test]
    public async Task UnregisterResponsePhaseRule_RegisteredRule_RemovesFromSnapshot()
    {
        var registry = new RuleRegistry();
        var rule = new StubResponseRule(priority: 1);
        registry.RegisterResponsePhaseRule(rule);

        registry.UnregisterResponsePhaseRule(rule);

        await Assert.That(registry.GetResponsePhaseRules().Count).IsEqualTo(0);
    }

    /// <summary>
    ///     Registering a rule raises the <see cref="IRuleRegistry.Changed" /> event.
    /// </summary>
    [Test]
    public async Task RegisterRequestPhaseRule_AnyRule_RaisesChangedEvent()
    {
        var registry = new RuleRegistry();
        var count = 0;
        registry.Changed += () => count++;

        registry.RegisterRequestPhaseRule(new StubRequestRule(priority: 1));

        await Assert.That(count).IsEqualTo(1);
    }

    /// <summary>
    ///     Registering a response-phase rule raises the <see cref="IRuleRegistry.Changed" /> event.
    /// </summary>
    [Test]
    public async Task RegisterResponsePhaseRule_AnyRule_RaisesChangedEvent()
    {
        var registry = new RuleRegistry();
        var count = 0;
        registry.Changed += () => count++;

        registry.RegisterResponsePhaseRule(new StubResponseRule(priority: 1));

        await Assert.That(count).IsEqualTo(1);
    }

    /// <summary>
    ///     Unregistering a rule that was registered raises the <see cref="IRuleRegistry.Changed" /> event.
    /// </summary>
    [Test]
    public async Task UnregisterRequestPhaseRule_RegisteredRule_RaisesChangedEvent()
    {
        var registry = new RuleRegistry();
        var rule = new StubRequestRule(priority: 1);
        registry.RegisterRequestPhaseRule(rule);
        var count = 0;
        registry.Changed += () => count++;

        registry.UnregisterRequestPhaseRule(rule);

        await Assert.That(count).IsEqualTo(1);
    }

    /// <summary>
    ///     Unregistering a rule that was never registered does NOT raise <see cref="IRuleRegistry.Changed" />.
    /// </summary>
    [Test]
    public async Task UnregisterRequestPhaseRule_UnknownRule_DoesNotRaiseChangedEvent()
    {
        var registry = new RuleRegistry();
        var count = 0;
        registry.Changed += () => count++;

        registry.UnregisterRequestPhaseRule(new StubRequestRule(priority: 1));

        await Assert.That(count).IsEqualTo(0);
    }

    /// <summary>
    ///     Unregistering a response-phase rule that was never registered does NOT raise <see cref="IRuleRegistry.Changed" />.
    /// </summary>
    [Test]
    public async Task UnregisterResponsePhaseRule_UnknownRule_DoesNotRaiseChangedEvent()
    {
        var registry = new RuleRegistry();
        var count = 0;
        registry.Changed += () => count++;

        registry.UnregisterResponsePhaseRule(new StubResponseRule(priority: 1));

        await Assert.That(count).IsEqualTo(0);
    }

    /// <summary>
    ///     Concurrent register/unregister operations on independent rule lists remain thread-safe.
    /// </summary>
    [Test]
    public async Task RegisterAndUnregister_FromMultipleTasks_IsThreadSafe()
    {
        var registry = new RuleRegistry();
        const int iterations = 200;

        var registerTask = Task.Run(() =>
        {
            for (var index = 0; index < iterations; index++)
            {
                registry.RegisterRequestPhaseRule(new StubRequestRule(priority: index));
            }
        });

        var queryTask = Task.Run(() =>
        {
            for (var index = 0; index < iterations; index++)
            {
                _ = registry.GetRequestPhaseRules();
            }
        });

        await Task.WhenAll(registerTask, queryTask);

        await Assert.That(registry.GetRequestPhaseRules().Count).IsEqualTo(iterations);
    }

    private sealed class StubRequestRule : IRequestPhaseRule
    {
        public bool IsEnabled { get; }

        public int Priority { get; }

        public StubRequestRule(int priority, bool isEnabled = true)
        {
            Priority = priority;
            IsEnabled = isEnabled;
        }

        public RequestPipelineAction? EvaluateRequest(HypertextTransferProtocolRequestData request)
        {
            return null;
        }
    }

    private sealed class StubResponseRule : IResponsePhaseRule
    {
        public bool IsEnabled { get; }

        public int Priority { get; }

        public StubResponseRule(int priority, bool isEnabled = true)
        {
            Priority = priority;
            IsEnabled = isEnabled;
        }

        public ResponsePipelineAction? EvaluateResponse(
            HypertextTransferProtocolRequestData request,
            HypertextTransferProtocolResponseData response)
        {
            return null;
        }
    }
}
