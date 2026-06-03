using Proxyfan.Domain.Rules.Matching;
using Proxyfan.Domain.Rules.Pipeline;
using Proxyfan.Domain.Rules.Rules;
using Proxyfan.Domain.Traffic;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
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
    ///     AddEntry that fails matcher compilation leaves the existing entry collection unchanged
    ///     so the rule's compiled state cannot drift away from its declared entries.
    /// </summary>
    [Test]
    public async Task AddEntry_MatcherCompilationFails_LeavesStateUnchanged()
    {
        var rule = new MutableMapLocalRule(priority: 300, isEnabled: true);
        var changedCount = 0;
        rule.Changed += () => changedCount++;
        var invalidEntry = new MapLocalEntry
        {
            Body = Array.Empty<byte>(),
            Headers = Array.Empty<KeyValuePair<string, string>>(),
            IsEnabled = true,
            MatchingRule = new MatchingRule("([unterminated", MatchingRuleKind.Regex),
            ReasonPhrase = "OK",
            StatusCode = 200,
        };

        await Assert.That(() => rule.AddEntry(invalidEntry)).Throws<RegexParseException>();

        await Assert.That(rule.GetEntries().Count).IsEqualTo(0);
        await Assert.That(changedCount).IsEqualTo(0);

        // Subsequent valid edits must continue to succeed and be served.
        rule.AddEntry(new MapLocalEntry
        {
            Body = Array.Empty<byte>(),
            Headers = Array.Empty<KeyValuePair<string, string>>(),
            IsEnabled = true,
            MatchingRule = new MatchingRule("https://stub.example.com/*", MatchingRuleKind.Wildcard),
            ReasonPhrase = "OK",
            StatusCode = 200,
        });
        var action = rule.EvaluateRequest(CreateRequest("https://stub.example.com/path"));

        await Assert.That(action).IsTypeOf<RequestPipelineAction.ServeLocalResponse>();
        await Assert.That(rule.GetEntries().Count).IsEqualTo(1);
        await Assert.That(changedCount).IsEqualTo(1);
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
    ///     Adding an entry whose matcher fails to compile leaves the entry list and
    ///     the compiled pipeline unchanged, and does not raise <see cref="MutableMapLocalRule.Changed" />.
    /// </summary>
    [Test]
    public async Task AddEntry_CompileFails_EntriesAndCompiledAreUnchanged()
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
        var count = 0;
        rule.Changed += () => count++;

        // An invalid regex pattern causes Compile() to throw.
        var invalidEntry = new MapLocalEntry
        {
            Body = Array.Empty<byte>(),
            Headers = Array.Empty<KeyValuePair<string, string>>(),
            IsEnabled = true,
            MatchingRule = new MatchingRule("[invalid-regex", MatchingRuleKind.Regex),
            ReasonPhrase = "OK",
            StatusCode = 200,
        };

        await Assert.That(() => rule.AddEntry(invalidEntry)).Throws<Exception>();
        await Assert.That(rule.GetEntries().Count).IsEqualTo(1);
        await Assert.That(count).IsEqualTo(0);
        var request = CreateRequest("https://stub.example.com/api");
        await Assert.That(rule.EvaluateRequest(request)).IsTypeOf<RequestPipelineAction.ServeLocalResponse>();
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
