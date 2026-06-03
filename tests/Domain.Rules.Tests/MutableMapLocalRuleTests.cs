using Proxyfan.Domain.Rules.Matching;
using Proxyfan.Domain.Rules.Pipeline;
using Proxyfan.Domain.Rules.Rules;
using Proxyfan.Domain.Traffic;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Proxyfan.Domain.Rules.Tests;

/// <summary>
///     Tests for <see cref="MutableMapLocalRule" />.
/// </summary>
public sealed class MutableMapLocalRuleTests
{
    /// <summary>
    ///     An empty rule does not match any URL.
    /// </summary>
    [Test]
    public async Task EvaluateRequest_NoEntries_ReturnsNull()
    {
        var rule = new MutableMapLocalRule(priority: 300, isEnabled: true);
        var request = CreateRequest("https://example.com/");

        var action = rule.EvaluateRequest(request);

        await Assert.That(action).IsNull();
    }

    /// <summary>
    ///     A matching entry returns the configured local response.
    /// </summary>
    [Test]
    public async Task EvaluateRequest_MatchingEntry_ReturnsServeLocalResponse()
    {
        var rule = new MutableMapLocalRule(priority: 300, isEnabled: true);
        var body = Encoding.UTF8.GetBytes("hello");
        var headers = new List<KeyValuePair<string, string>>
        {
            new("Content-Type", "text/plain"),
        };
        rule.AddEntry(new MapLocalEntry
        {
            Body = body,
            Headers = headers,
            IsEnabled = true,
            MatchingRule = new MatchingRule("https://stub.example.com/*", MatchingRuleKind.Wildcard),
            ReasonPhrase = "OK",
            StatusCode = 200,
        });
        var request = CreateRequest("https://stub.example.com/api/data");

        var action = rule.EvaluateRequest(request);

        await Assert.That(action).IsTypeOf<RequestPipelineAction.ServeLocalResponse>();
        var serve = (RequestPipelineAction.ServeLocalResponse)action!;
        await Assert.That(serve.LocalResponse.StatusCode).IsEqualTo(200);
        await Assert.That(serve.LocalResponse.ReasonPhrase).IsEqualTo("OK");
        await Assert.That(serve.LocalResponse.Body.Length).IsEqualTo(5);
        await Assert.That(serve.LocalResponse.Headers.GetAll("Content-Type")[0]).IsEqualTo("text/plain");
    }

    /// <summary>
    ///     A disabled entry is skipped during evaluation.
    /// </summary>
    [Test]
    public async Task EvaluateRequest_DisabledEntry_IsSkipped()
    {
        var rule = new MutableMapLocalRule(priority: 300, isEnabled: true);
        rule.AddEntry(new MapLocalEntry
        {
            Body = Array.Empty<byte>(),
            Headers = Array.Empty<KeyValuePair<string, string>>(),
            IsEnabled = false,
            MatchingRule = new MatchingRule("https://stub.example.com/*", MatchingRuleKind.Wildcard),
            ReasonPhrase = "OK",
            StatusCode = 200,
        });
        var request = CreateRequest("https://stub.example.com/path");

        var action = rule.EvaluateRequest(request);

        await Assert.That(action).IsNull();
    }

    /// <summary>
    ///     A non-matching URL returns null.
    /// </summary>
    [Test]
    public async Task EvaluateRequest_NonMatchingUrl_ReturnsNull()
    {
        var rule = new MutableMapLocalRule(priority: 300, isEnabled: true);
        rule.AddEntry(new MapLocalEntry
        {
            Body = Array.Empty<byte>(),
            Headers = Array.Empty<KeyValuePair<string, string>>(),
            IsEnabled = true,
            MatchingRule = new MatchingRule("https://stub.example.com/*", MatchingRuleKind.Wildcard),
            ReasonPhrase = "OK",
            StatusCode = 200,
        });
        var request = CreateRequest("https://other.example.com/path");

        var action = rule.EvaluateRequest(request);

        await Assert.That(action).IsNull();
    }

    /// <summary>
    ///     AddEntry with an invalid status code throws.
    /// </summary>
    [Test]
    public async Task AddEntry_InvalidStatusCode_Throws()
    {
        var rule = new MutableMapLocalRule(priority: 300, isEnabled: true);

        var entry = new MapLocalEntry
        {
            Body = Array.Empty<byte>(),
            Headers = Array.Empty<KeyValuePair<string, string>>(),
            IsEnabled = true,
            MatchingRule = new MatchingRule("https://stub.example.com/*", MatchingRuleKind.Wildcard),
            ReasonPhrase = "OK",
            StatusCode = 99,
        };

        await Assert.That(() => rule.AddEntry(entry)).Throws<ArgumentOutOfRangeException>();
    }

    /// <summary>
    ///     Adding an entry whose matching rule fails to compile leaves <c>_entries</c> and
    ///     <c>_compiled</c> in their original state and does not raise <see cref="MutableMapLocalRule.Changed" />.
    /// </summary>
    [Test]
    public async Task AddEntry_CompilationFailure_DoesNotLeaveStaleState()
    {
        var rule = new MutableMapLocalRule(priority: 300, isEnabled: true);
        var good = new MapLocalEntry
        {
            Body = Array.Empty<byte>(),
            Headers = Array.Empty<KeyValuePair<string, string>>(),
            IsEnabled = true,
            MatchingRule = new MatchingRule("https://stub.example.com/*", MatchingRuleKind.Wildcard),
            ReasonPhrase = "OK",
            StatusCode = 200,
        };
        rule.AddEntry(good);

        var changedCount = 0;
        rule.Changed += () => changedCount++;

        var bad = new MapLocalEntry
        {
            Body = Array.Empty<byte>(),
            Headers = Array.Empty<KeyValuePair<string, string>>(),
            IsEnabled = true,
            MatchingRule = new MatchingRule("[invalid-regex", MatchingRuleKind.Regex),
            ReasonPhrase = "OK",
            StatusCode = 200,
        };

        await Assert.That(() => rule.AddEntry(bad)).Throws<Exception>();
        await Assert.That(rule.GetEntries().Count).IsEqualTo(1);
        await Assert.That(changedCount).IsEqualTo(0);

        // The rule must still evaluate correctly after the failed add.
        var request = CreateRequest("https://stub.example.com/test");
        var action = rule.EvaluateRequest(request);
        await Assert.That(action).IsTypeOf<RequestPipelineAction.ServeLocalResponse>();
    }

    /// <summary>
    ///     Adding an entry raises the <see cref="MutableMapLocalRule.Changed" /> event.
    /// </summary>
    [Test]
    public async Task AddEntry_OnAdd_RaisesChanged()
    {
        var rule = new MutableMapLocalRule(priority: 300, isEnabled: true);
        var count = 0;
        rule.Changed += () => count++;

        rule.AddEntry(new MapLocalEntry
        {
            Body = Array.Empty<byte>(),
            Headers = Array.Empty<KeyValuePair<string, string>>(),
            IsEnabled = true,
            MatchingRule = new MatchingRule("https://stub.example.com/*", MatchingRuleKind.Wildcard),
            ReasonPhrase = "OK",
            StatusCode = 200,
        });

        await Assert.That(count).IsEqualTo(1);
        await Assert.That(rule.GetEntries().Count).IsEqualTo(1);
    }

    /// <summary>
    ///     Removing a registered entry raises Changed and removes the entry.
    /// </summary>
    [Test]
    public async Task RemoveEntry_RegisteredEntry_RemovesAndRaisesChanged()
    {
        var rule = new MutableMapLocalRule(priority: 300, isEnabled: true);
        var entry = new MapLocalEntry
        {
            Body = Array.Empty<byte>(),
            Headers = Array.Empty<KeyValuePair<string, string>>(),
            IsEnabled = true,
            MatchingRule = new MatchingRule("https://stub.example.com/*", MatchingRuleKind.Wildcard),
            ReasonPhrase = "OK",
            StatusCode = 200,
        };
        rule.AddEntry(entry);
        var count = 0;
        rule.Changed += () => count++;

        rule.RemoveEntry(entry);

        await Assert.That(count).IsEqualTo(1);
        await Assert.That(rule.GetEntries().Count).IsEqualTo(0);
    }

    /// <summary>
    ///     Removing an unknown entry is a no-op.
    /// </summary>
    [Test]
    public async Task RemoveEntry_UnknownEntry_NoOp()
    {
        var rule = new MutableMapLocalRule(priority: 300, isEnabled: true);
        var count = 0;
        rule.Changed += () => count++;

        rule.RemoveEntry(new MapLocalEntry
        {
            Body = Array.Empty<byte>(),
            Headers = Array.Empty<KeyValuePair<string, string>>(),
            IsEnabled = true,
            MatchingRule = new MatchingRule("https://x.example.com/*", MatchingRuleKind.Wildcard),
            ReasonPhrase = "OK",
            StatusCode = 200,
        });

        await Assert.That(count).IsEqualTo(0);
    }

    /// <summary>
    ///     SetEnabled changes the IsEnabled state and raises Changed.
    /// </summary>
    [Test]
    public async Task SetEnabled_DifferentValue_RaisesChanged()
    {
        var rule = new MutableMapLocalRule(priority: 300, isEnabled: false);
        var count = 0;
        rule.Changed += () => count++;

        rule.SetEnabled(isEnabled: true);

        await Assert.That(count).IsEqualTo(1);
        await Assert.That(rule.IsEnabled).IsTrue();
    }

    /// <summary>
    ///     SetEnabled with same value does not raise Changed.
    /// </summary>
    [Test]
    public async Task SetEnabled_SameValue_DoesNotRaiseChanged()
    {
        var rule = new MutableMapLocalRule(priority: 300, isEnabled: false);
        var count = 0;
        rule.Changed += () => count++;

        rule.SetEnabled(isEnabled: false);

        await Assert.That(count).IsEqualTo(0);
    }

    /// <summary>
    ///     GetEntries returns a defensive snapshot.
    /// </summary>
    [Test]
    public async Task GetEntries_AfterMutation_ReturnsDefensiveSnapshot()
    {
        var rule = new MutableMapLocalRule(priority: 300, isEnabled: true);
        rule.AddEntry(new MapLocalEntry
        {
            Body = Array.Empty<byte>(),
            Headers = Array.Empty<KeyValuePair<string, string>>(),
            IsEnabled = true,
            MatchingRule = new MatchingRule("https://a.example.com/*", MatchingRuleKind.Wildcard),
            ReasonPhrase = "OK",
            StatusCode = 200,
        });

        var snapshot = rule.GetEntries();
        rule.AddEntry(new MapLocalEntry
        {
            Body = Array.Empty<byte>(),
            Headers = Array.Empty<KeyValuePair<string, string>>(),
            IsEnabled = true,
            MatchingRule = new MatchingRule("https://b.example.com/*", MatchingRuleKind.Wildcard),
            ReasonPhrase = "OK",
            StatusCode = 200,
        });

        await Assert.That(snapshot.Count).IsEqualTo(1);
        await Assert.That(rule.GetEntries().Count).IsEqualTo(2);
    }

    /// <summary>
    ///     Priority reflects the constructor argument.
    /// </summary>
    [Test]
    public async Task Priority_AfterConstruction_ReturnsConstructorValue()
    {
        var rule = new MutableMapLocalRule(priority: 305, isEnabled: true);

        await Assert.That(rule.Priority).IsEqualTo(305);
    }

    private static HypertextTransferProtocolRequestData CreateRequest(string url)
    {
        var parameters = new HypertextTransferProtocolRequestDataParameters
        {
            Body = Array.Empty<byte>(),
            Headers = HeaderCollection.Empty.Add("Host", new Uri(url).Host),
            Method = "GET",
            RequestUri = new Uri(url),
            Version = "HTTP/1.1",
        };
        return new HypertextTransferProtocolRequestData(parameters);
    }
}
