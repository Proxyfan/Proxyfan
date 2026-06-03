using Proxyfan.Domain.Rules.Matching;
using Proxyfan.Domain.Rules.Pipeline;
using Proxyfan.Domain.Rules.Rules;
using Proxyfan.Domain.Traffic;
using System;
using System.Threading.Tasks;

namespace Proxyfan.Domain.Rules.Tests;

/// <summary>
///     Tests for <see cref="MutableMapRemoteRule" />.
/// </summary>
public sealed class MutableMapRemoteRuleTests
{
    /// <summary>
    ///     An empty rule does not redirect any URL.
    /// </summary>
    [Test]
    public async Task EvaluateRequest_NoEntries_ReturnsNull()
    {
        var rule = new MutableMapRemoteRule(priority: 200, isEnabled: true);
        var request = CreateRequest("https://example.com/path");

        var action = rule.EvaluateRequest(request);

        await Assert.That(action).IsNull();
    }

    /// <summary>
    ///     A matching entry returns a <see cref="RequestPipelineAction.Redirect" /> with the rewritten URI.
    /// </summary>
    [Test]
    public async Task EvaluateRequest_MatchingEntry_ReturnsRedirectWithRewrittenUri()
    {
        var rule = new MutableMapRemoteRule(priority: 200, isEnabled: true);
        var destination = new MapRemoteDestination(scheme: "https", host: "internal.example.com", port: 8443, path: null, isPreservingHostHeader: false);
        var entry = new MapRemoteEntry
        {
            Destination = destination,
            IsEnabled = true,
            MatchingRule = new MatchingRule("https://public.example.com/*", MatchingRuleKind.Wildcard),
        };
        rule.AddEntry(entry);
        var request = CreateRequest("https://public.example.com/api/users");

        var action = rule.EvaluateRequest(request);

        await Assert.That(action).IsTypeOf<RequestPipelineAction.Redirect>();
        var redirect = (RequestPipelineAction.Redirect)action!;
        await Assert.That(redirect.RewrittenRequest.RequestUri.Host).IsEqualTo("internal.example.com");
        await Assert.That(redirect.RewrittenRequest.RequestUri.Port).IsEqualTo(8443);
        await Assert.That(redirect.RewrittenRequest.RequestUri.AbsolutePath).IsEqualTo("/api/users");
    }

    /// <summary>
    ///     A non-matching URL returns null.
    /// </summary>
    [Test]
    public async Task EvaluateRequest_NonMatchingUrl_ReturnsNull()
    {
        var rule = new MutableMapRemoteRule(priority: 200, isEnabled: true);
        var destination = new MapRemoteDestination(scheme: "https", host: "internal.example.com", port: null, path: null, isPreservingHostHeader: false);
        var entry = new MapRemoteEntry
        {
            Destination = destination,
            IsEnabled = true,
            MatchingRule = new MatchingRule("https://public.example.com/*", MatchingRuleKind.Wildcard),
        };
        rule.AddEntry(entry);
        var request = CreateRequest("https://other.example.com/path");

        var action = rule.EvaluateRequest(request);

        await Assert.That(action).IsNull();
    }

    /// <summary>
    ///     A disabled entry is skipped during evaluation.
    /// </summary>
    [Test]
    public async Task EvaluateRequest_DisabledEntry_IsSkipped()
    {
        var rule = new MutableMapRemoteRule(priority: 200, isEnabled: true);
        var destination = new MapRemoteDestination(scheme: "https", host: "internal.example.com", port: null, path: null, isPreservingHostHeader: false);
        var entry = new MapRemoteEntry
        {
            Destination = destination,
            IsEnabled = false,
            MatchingRule = new MatchingRule("https://public.example.com/*", MatchingRuleKind.Wildcard),
        };
        rule.AddEntry(entry);
        var request = CreateRequest("https://public.example.com/path");

        var action = rule.EvaluateRequest(request);

        await Assert.That(action).IsNull();
    }

    /// <summary>
    ///     Preserving the Host header keeps the original Host header value after URI rewrite.
    /// </summary>
    [Test]
    public async Task EvaluateRequest_PreserveHostHeader_RetainsOriginalHost()
    {
        var rule = new MutableMapRemoteRule(priority: 200, isEnabled: true);
        var destination = new MapRemoteDestination(scheme: "https", host: "internal.example.com", port: null, path: null, isPreservingHostHeader: true);
        var entry = new MapRemoteEntry
        {
            Destination = destination,
            IsEnabled = true,
            MatchingRule = new MatchingRule("https://public.example.com/*", MatchingRuleKind.Wildcard),
        };
        rule.AddEntry(entry);
        var request = CreateRequest("https://public.example.com/api");

        var action = rule.EvaluateRequest(request);

        var redirect = (RequestPipelineAction.Redirect)action!;
        var host = redirect.RewrittenRequest.Headers.GetAll("Host");
        await Assert.That(host.Length).IsEqualTo(1);
        await Assert.That(host[0]).IsEqualTo("public.example.com");
    }

    /// <summary>
    ///     Adding an entry raises the <see cref="MutableMapRemoteRule.Changed" /> event.
    /// </summary>
    [Test]
    public async Task AddEntry_OnAdd_RaisesChanged()
    {
        var rule = new MutableMapRemoteRule(priority: 200, isEnabled: true);
        var count = 0;
        rule.Changed += () => count++;

        var destination = new MapRemoteDestination(scheme: "https", host: "internal.example.com", port: null, path: null, isPreservingHostHeader: false);
        rule.AddEntry(new MapRemoteEntry
        {
            Destination = destination,
            IsEnabled = true,
            MatchingRule = new MatchingRule("https://public.example.com/*", MatchingRuleKind.Wildcard),
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
        var rule = new MutableMapRemoteRule(priority: 200, isEnabled: true);
        var destination = new MapRemoteDestination(scheme: "https", host: "internal.example.com", port: null, path: null, isPreservingHostHeader: false);
        var entry = new MapRemoteEntry
        {
            Destination = destination,
            IsEnabled = true,
            MatchingRule = new MatchingRule("https://public.example.com/*", MatchingRuleKind.Wildcard),
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
        var rule = new MutableMapRemoteRule(priority: 200, isEnabled: true);
        var count = 0;
        rule.Changed += () => count++;

        var destination = new MapRemoteDestination(scheme: "https", host: "internal.example.com", port: null, path: null, isPreservingHostHeader: false);
        rule.RemoveEntry(new MapRemoteEntry
        {
            Destination = destination,
            IsEnabled = true,
            MatchingRule = new MatchingRule("https://nothing.example.com/*", MatchingRuleKind.Wildcard),
        });

        await Assert.That(count).IsEqualTo(0);
    }

    /// <summary>
    ///     SetEnabled changes the IsEnabled state and raises Changed.
    /// </summary>
    [Test]
    public async Task SetEnabled_DifferentValue_RaisesChanged()
    {
        var rule = new MutableMapRemoteRule(priority: 200, isEnabled: false);
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
        var rule = new MutableMapRemoteRule(priority: 200, isEnabled: false);
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
        var rule = new MutableMapRemoteRule(priority: 200, isEnabled: true);
        var destination = new MapRemoteDestination(scheme: "https", host: "internal.example.com", port: null, path: null, isPreservingHostHeader: false);
        rule.AddEntry(new MapRemoteEntry
        {
            Destination = destination,
            IsEnabled = true,
            MatchingRule = new MatchingRule("https://a.example.com/*", MatchingRuleKind.Wildcard),
        });

        var snapshot = rule.GetEntries();
        rule.AddEntry(new MapRemoteEntry
        {
            Destination = destination,
            IsEnabled = true,
            MatchingRule = new MatchingRule("https://b.example.com/*", MatchingRuleKind.Wildcard),
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
        var rule = new MutableMapRemoteRule(priority: 250, isEnabled: true);

        await Assert.That(rule.Priority).IsEqualTo(250);
    }

    /// <summary>
    ///     When <see cref="MatchingRule.Compile" /> throws, <see cref="MutableMapRemoteRule.AddEntry" />
    ///     must not mutate <c>_entries</c>, must not update <c>_compiled</c>, and must not raise
    ///     <see cref="MutableMapRemoteRule.Changed" />.
    /// </summary>
    [Test]
    public async Task AddEntry_CompileThrows_LeavesStateUnchanged()
    {
        var rule = new MutableMapRemoteRule(priority: 200, isEnabled: true);
        var destination = new MapRemoteDestination(scheme: "https", host: "internal.example.com", port: null, path: null, isPreservingHostHeader: false);
        var good = new MapRemoteEntry
        {
            Destination = destination,
            IsEnabled = true,
            MatchingRule = new MatchingRule("https://public.example.com/*", MatchingRuleKind.Wildcard),
        };
        rule.AddEntry(good);

        var count = 0;
        rule.Changed += () => count++;

        var invalid = new MapRemoteEntry
        {
            Destination = destination,
            IsEnabled = true,
            MatchingRule = new MatchingRule("irrelevant", (MatchingRuleKind)999),
        };

        await Assert.That(() => rule.AddEntry(invalid)).Throws<InvalidOperationException>();

        await Assert.That(count).IsEqualTo(0);
        await Assert.That(rule.GetEntries().Count).IsEqualTo(1);

        var request = CreateRequest("https://public.example.com/api");
        var action = rule.EvaluateRequest(request);
        await Assert.That(action).IsTypeOf<RequestPipelineAction.Redirect>();
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
