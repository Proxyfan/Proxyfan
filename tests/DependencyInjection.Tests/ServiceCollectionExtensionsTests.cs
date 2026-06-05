using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Proxyfan.DependencyInjection;
using Proxyfan.Domain;
using Proxyfan.Domain.Proxy;
using Proxyfan.Domain.Rules.Pipeline;
using Proxyfan.Domain.Rules.Matching;
using Proxyfan.Domain.Rules.Rules;
using Proxyfan.Domain.Scripting;
using Proxyfan.Domain.Session.Har;
using Proxyfan.Domain.Rules;
using Proxyfan.Domain.Traffic;
using Proxyfan.Plugin.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Proxyfan.DependencyInjection.Tests;

/// <summary>
///     Tests for <see cref="ServiceCollectionExtensions" />.
///     Covers service registration behavior for proxy infrastructure and interface scanning.
/// </summary>
public sealed class ServiceCollectionExtensionsTests
{
    /// <summary>
    ///     Verifies that <see cref="ServiceCollectionExtensions.AddProxyListener" /> registers the core proxy services.
    ///     Also verifies that the required connection handlers are available from the built provider.
    /// </summary>
    [Test]
    public async Task AddProxyListener_WhenCalled_RegistersRequiredServices()
    {
        var services = new ServiceCollection();
        var configurationBuilder = new ConfigurationBuilder();
        var configuration = configurationBuilder.Build();
        services.AddLogging();
        services.AddSingleton<IDomainEventBus, DomainEventBus>();
        services.AddProxyListener(configuration);

        using ServiceProvider provider = services.BuildServiceProvider();
        var listener = provider.GetService<IProxyListener>();
        var dispatcher = provider.GetService<IConnectionDispatcher>();
        var trafficStore = provider.GetService<ITrafficStore>();
        var systemProxy = provider.GetService<ISystemProxy>();
        var ruleEngine = provider.GetService<IRuleEngine>();
        var handlers = provider.GetServices<IConnectionHandler>().ToArray();

        await Assert.That(listener).IsNotNull();
        await Assert.That(dispatcher).IsNotNull();
        await Assert.That(trafficStore).IsNotNull();
        await Assert.That(systemProxy).IsNotNull();
        await Assert.That(ruleEngine).IsNotNull();
        await Assert.That(handlers).Count().IsEqualTo(3);
    }

    /// <summary>
    ///     Verifies that <see cref="ServiceCollectionExtensions.AddSessionHarServices" /> wires
    ///     the HAR import/export abstractions.
    /// </summary>
    [Test]
    public async Task AddSessionHarServices_WhenCalled_RegistersHarAbstractions()
    {
        var services = new ServiceCollection();
        services.AddSessionHarServices();

        using ServiceProvider provider = services.BuildServiceProvider();
        var exporter = provider.GetService<IHarExporter>();
        var importer = provider.GetService<IHarImporter>();

        await Assert.That(exporter).IsNotNull();
        await Assert.That(importer).IsNotNull();
    }

    /// <summary>
    ///     Verifies that the production rule engine registers the documented request/response rules
    ///     and evaluates them in the expected order.
    /// </summary>
    [Test]
    public async Task AddProxyListener_RuleEngineResolved_EvaluatesDocumentedOrder()
    {
        var breakpointHandler = new StubBreakpointHandler
        {
            RequestHeaderName = "X-Breakpoint",
            RequestHeaderValue = "request",
            ResponseHeaderName = "X-Breakpoint-Response",
            ResponseHeaderValue = "response",
        };
        var scriptingHandler = new StubScriptingHandler
        {
            RequestHeaderName = "X-Script",
            RequestHeaderValue = "request",
            ResponseHeaderName = "X-Script-Response",
            ResponseHeaderValue = "response",
        };

        using ServiceProvider provider = CreateProvider(breakpointHandler, scriptingHandler);
        var engine = provider.GetRequiredService<IRuleEngine>();
        var registry = provider.GetRequiredService<IRuleRegistry>();
        var allowList = provider.GetRequiredService<MutableAllowListRule>();
        var blockList = provider.GetRequiredService<MutableBlockListRule>();
        var mapRemote = provider.GetRequiredService<MutableMapRemoteRule>();
        var mapLocal = provider.GetRequiredService<MutableMapLocalRule>();
        var noCachingRule = provider.GetRequiredService<MutableNoCachingRule>();

        var requestPhaseRuleNames = registry.GetRequestPhaseRules().Select(static rule => rule.GetType().Name).ToArray();
        var asyncRequestPhaseRuleNames = registry.GetAsyncRequestPhaseRules().Select(static rule => rule.GetType().Name).ToArray();
        var responsePhaseRuleNames = registry.GetResponsePhaseRules().Select(static rule => rule.GetType().Name).ToArray();
        var asyncResponsePhaseRuleNames = registry.GetAsyncResponsePhaseRules().Select(static rule => rule.GetType().Name).ToArray();

        await Assert.That(engine).IsNotNull();
        await Assert.That(requestPhaseRuleNames.Length).IsEqualTo(5);
        await Assert.That(requestPhaseRuleNames[0]).IsEqualTo(nameof(MutableAllowListRule));
        await Assert.That(requestPhaseRuleNames[1]).IsEqualTo(nameof(MutableBlockListRule));
        await Assert.That(requestPhaseRuleNames[2]).IsEqualTo(nameof(MutableMapRemoteRule));
        await Assert.That(requestPhaseRuleNames[3]).IsEqualTo(nameof(MutableMapLocalRule));
        await Assert.That(requestPhaseRuleNames[4]).IsEqualTo(nameof(MutableNoCachingRule));
        await Assert.That(asyncRequestPhaseRuleNames.Length).IsEqualTo(2);
        await Assert.That(asyncRequestPhaseRuleNames[0]).IsEqualTo(nameof(BreakpointRule));
        await Assert.That(asyncRequestPhaseRuleNames[1]).IsEqualTo(nameof(ScriptingRule));
        await Assert.That(responsePhaseRuleNames.Length).IsEqualTo(1);
        await Assert.That(responsePhaseRuleNames[0]).IsEqualTo(nameof(MutableNoCachingRule));
        await Assert.That(asyncResponsePhaseRuleNames.Length).IsEqualTo(2);
        await Assert.That(asyncResponsePhaseRuleNames[0]).IsEqualTo(nameof(ScriptingRule));
        await Assert.That(asyncResponsePhaseRuleNames[1]).IsEqualTo(nameof(BreakpointRule));

        allowList.SetEnabled(isEnabled: true);
        allowList.AddPattern(new MatchingRule("https://public.example.com/*", MatchingRuleKind.Wildcard));
        blockList.AddPattern(new MatchingRule("https://blocked.example.com/*", MatchingRuleKind.Wildcard));
        mapRemote.AddEntry(new MapRemoteEntry
        {
            Destination = new MapRemoteDestination(
                scheme: "https",
                host: "rewritten.example.com",
                port: 8443,
                path: null,
                isPreservingHostHeader: false),
            IsEnabled = true,
            MatchingRule = new MatchingRule("https://public.example.com/*", MatchingRuleKind.Wildcard),
        });
        mapLocal.AddEntry(new MapLocalEntry
        {
            Body = Array.Empty<byte>(),
            Headers = Array.Empty<KeyValuePair<string, string>>(),
            IsEnabled = true,
            MatchingRule = new MatchingRule("https://local-only.example.com/*", MatchingRuleKind.Wildcard),
            ReasonPhrase = "OK",
            StatusCode = 200,
        });
        noCachingRule.SetEnabled(isEnabled: true);

        var request = CreateRequest(
            "https://public.example.com/api/orders",
            HeaderCollection.Empty
                .Add("Host", "public.example.com")
                .Add("If-None-Match", "\"etag-1\""));

        var requestActions = await engine.EvaluateRequestAsync(request, flowId: "flow-order", CancellationToken.None);

        await Assert.That(requestActions.Count).IsEqualTo(4);
        await Assert.That(requestActions[0]).IsTypeOf<RequestPipelineAction.Redirect>();
        await Assert.That(requestActions[1]).IsTypeOf<RequestPipelineAction.ModifyRequest>();
        await Assert.That(requestActions[2]).IsTypeOf<RequestPipelineAction.ModifyRequest>();
        await Assert.That(requestActions[3]).IsTypeOf<RequestPipelineAction.ModifyRequest>();
        await Assert.That(breakpointHandler.SeenRequests.Count).IsEqualTo(1);
        await Assert.That(breakpointHandler.SeenRequests[0].RequestUri.ToString()).IsEqualTo("https://rewritten.example.com:8443/api/orders");
        await Assert.That(scriptingHandler.SeenRequests.Count).IsEqualTo(1);
        await Assert.That(scriptingHandler.SeenRequests[0].Headers.GetAll("X-Breakpoint")[0]).IsEqualTo("request");
        var finalRequest = ((RequestPipelineAction.ModifyRequest)requestActions[3]).ModifiedRequest;
        await Assert.That(finalRequest.RequestUri.ToString()).IsEqualTo("https://rewritten.example.com:8443/api/orders");
        await Assert.That(finalRequest.Headers.GetAll("X-Breakpoint")[0]).IsEqualTo("request");
        await Assert.That(finalRequest.Headers.GetAll("X-Script")[0]).IsEqualTo("request");
        await Assert.That(finalRequest.Headers.GetAll("If-None-Match").Length).IsEqualTo(0);
        await Assert.That(finalRequest.Headers.GetAll("Cache-Control")[0]).IsEqualTo("no-cache");

        var response = CreateResponse(
            statusCode: 200,
            HeaderCollection.Empty
                .Add("ETag", "\"etag-1\"")
                .Add("Cache-Control", "public, max-age=60"));

        var responseActions = await engine.EvaluateResponseAsync(finalRequest, response, flowId: "flow-order", CancellationToken.None);

        await Assert.That(responseActions.Count).IsEqualTo(3);
        await Assert.That(responseActions[0]).IsTypeOf<ResponsePipelineAction.ModifyResponse>();
        await Assert.That(responseActions[1]).IsTypeOf<ResponsePipelineAction.ModifyResponse>();
        await Assert.That(responseActions[2]).IsTypeOf<ResponsePipelineAction.ModifyResponse>();
        await Assert.That(scriptingHandler.SeenResponses.Count).IsEqualTo(1);
        await Assert.That(scriptingHandler.SeenResponses[0].Headers.GetAll("ETag").Length).IsEqualTo(0);
        await Assert.That(scriptingHandler.SeenResponses[0].Headers.GetAll("Cache-Control")[0]).IsEqualTo("no-cache, no-store, must-revalidate");
        await Assert.That(breakpointHandler.SeenResponses.Count).IsEqualTo(1);
        await Assert.That(breakpointHandler.SeenResponses[0].Headers.GetAll("X-Script-Response")[0]).IsEqualTo("response");
        var finalResponse = ((ResponsePipelineAction.ModifyResponse)responseActions[2]).ModifiedResponse;
        await Assert.That(finalResponse.Headers.GetAll("ETag").Length).IsEqualTo(0);
        await Assert.That(finalResponse.Headers.GetAll("Cache-Control")[0]).IsEqualTo("no-cache, no-store, must-revalidate");
        await Assert.That(finalResponse.Headers.GetAll("X-Script-Response")[0]).IsEqualTo("response");
        await Assert.That(finalResponse.Headers.GetAll("X-Breakpoint-Response")[0]).IsEqualTo("response");
    }

    /// <summary>
    ///     Verifies that an allow-list denial short-circuits the request phase before later rules run.
    /// </summary>
    [Test]
    public async Task AddProxyListener_RuleEngine_AllowListShortCircuitsBeforeBlock()
    {
        var breakpointHandler = new StubBreakpointHandler();
        var scriptingHandler = new StubScriptingHandler();

        using ServiceProvider provider = CreateProvider(breakpointHandler, scriptingHandler);
        var engine = provider.GetRequiredService<IRuleEngine>();
        var allowList = provider.GetRequiredService<MutableAllowListRule>();
        var blockList = provider.GetRequiredService<MutableBlockListRule>();
        var mapRemote = provider.GetRequiredService<MutableMapRemoteRule>();

        allowList.SetEnabled(isEnabled: true);
        allowList.AddPattern(new MatchingRule("https://allowed.example.com/*", MatchingRuleKind.Wildcard));
        blockList.AddPattern(new MatchingRule("https://denied.example.com/*", MatchingRuleKind.Wildcard));
        mapRemote.AddEntry(new MapRemoteEntry
        {
            Destination = new MapRemoteDestination(
                scheme: "https",
                host: "rewritten.example.com",
                port: null,
                path: null,
                isPreservingHostHeader: false),
            IsEnabled = true,
            MatchingRule = new MatchingRule("https://denied.example.com/*", MatchingRuleKind.Wildcard),
        });

        var actions = await engine.EvaluateRequestAsync(CreateRequest("https://denied.example.com/path"), flowId: "flow-allow", CancellationToken.None);

        await Assert.That(actions.Count).IsEqualTo(1);
        await Assert.That(actions[0]).IsTypeOf<RequestPipelineAction.Block>();
        await Assert.That(breakpointHandler.SeenRequests.Count).IsEqualTo(0);
        await Assert.That(scriptingHandler.SeenRequests.Count).IsEqualTo(0);
    }

    /// <summary>
    ///     Verifies that Map Local serves a local response after Map Remote rewriting and skips the
    ///     remaining request-phase rules.
    /// </summary>
    [Test]
    public async Task AddProxyListener_RuleEngine_MapLocalShortCircuitsBeforeUpstreamSend()
    {
        var breakpointHandler = new StubBreakpointHandler
        {
            ResponseHeaderName = "X-Breakpoint-Response",
            ResponseHeaderValue = "response",
        };
        var scriptingHandler = new StubScriptingHandler
        {
            ResponseHeaderName = "X-Script-Response",
            ResponseHeaderValue = "response",
        };

        using ServiceProvider provider = CreateProvider(breakpointHandler, scriptingHandler);
        var engine = provider.GetRequiredService<IRuleEngine>();
        var allowList = provider.GetRequiredService<MutableAllowListRule>();
        var mapRemote = provider.GetRequiredService<MutableMapRemoteRule>();
        var mapLocal = provider.GetRequiredService<MutableMapLocalRule>();
        var noCachingRule = provider.GetRequiredService<MutableNoCachingRule>();

        allowList.SetEnabled(isEnabled: true);
        allowList.AddPattern(new MatchingRule("https://public.example.com/*", MatchingRuleKind.Wildcard));
        mapRemote.AddEntry(new MapRemoteEntry
        {
            Destination = new MapRemoteDestination(
                scheme: "https",
                host: "local.example.com",
                port: null,
                path: null,
                isPreservingHostHeader: false),
            IsEnabled = true,
            MatchingRule = new MatchingRule("https://public.example.com/*", MatchingRuleKind.Wildcard),
        });
        mapLocal.AddEntry(new MapLocalEntry
        {
            Body = Array.Empty<byte>(),
            Headers = new[]
            {
                new KeyValuePair<string, string>("ETag", "\"local\""),
            },
            IsEnabled = true,
            MatchingRule = new MatchingRule("https://local.example.com/*", MatchingRuleKind.Wildcard),
            ReasonPhrase = "OK",
            StatusCode = 200,
        });
        noCachingRule.SetEnabled(isEnabled: true);

        var requestActions = await engine.EvaluateRequestAsync(CreateRequest("https://public.example.com/api"), flowId: "flow-local", CancellationToken.None);

        await Assert.That(requestActions.Count).IsEqualTo(2);
        await Assert.That(requestActions[0]).IsTypeOf<RequestPipelineAction.Redirect>();
        await Assert.That(requestActions[1]).IsTypeOf<RequestPipelineAction.ServeLocalResponse>();
        await Assert.That(breakpointHandler.SeenRequests.Count).IsEqualTo(0);
        await Assert.That(scriptingHandler.SeenRequests.Count).IsEqualTo(0);

        var rewrittenRequest = ((RequestPipelineAction.Redirect)requestActions[0]).RewrittenRequest;
        var localResponse = ((RequestPipelineAction.ServeLocalResponse)requestActions[1]).LocalResponse;
        var responseActions = await engine.EvaluateResponseAsync(rewrittenRequest, localResponse, flowId: "flow-local", CancellationToken.None);

        await Assert.That(responseActions.Count).IsEqualTo(3);
        await Assert.That(responseActions[0]).IsTypeOf<ResponsePipelineAction.ModifyResponse>();
        await Assert.That(responseActions[1]).IsTypeOf<ResponsePipelineAction.ModifyResponse>();
        await Assert.That(responseActions[2]).IsTypeOf<ResponsePipelineAction.ModifyResponse>();
        await Assert.That(scriptingHandler.SeenResponses.Count).IsEqualTo(1);
        await Assert.That(breakpointHandler.SeenResponses.Count).IsEqualTo(1);
        var finalResponse = ((ResponsePipelineAction.ModifyResponse)responseActions[2]).ModifiedResponse;
        await Assert.That(finalResponse.Headers.GetAll("ETag").Length).IsEqualTo(0);
        await Assert.That(finalResponse.Headers.GetAll("Cache-Control")[0]).IsEqualTo("no-cache, no-store, must-revalidate");
        await Assert.That(finalResponse.Headers.GetAll("X-Script-Response")[0]).IsEqualTo("response");
        await Assert.That(finalResponse.Headers.GetAll("X-Breakpoint-Response")[0]).IsEqualTo("response");
    }

    /// <summary>
    ///     Verifies that <see cref="ServiceCollectionExtensions.AddSingletonAsImplementedInterfaces{TImplementation}" />
    ///     registers every implemented interface except <see cref="IDisposable" />.
    /// </summary>
    [Test]
    public async Task AddSingletonAsImplementedInterfaces_WhenImplementationHasInterfaces_RegistersAllNonDisposableInterfaces()
    {
        var services = new ServiceCollection();
        services.AddSingletonAsImplementedInterfaces<SampleImplementation>(CreateSampleImplementation);

        using ServiceProvider provider = services.BuildServiceProvider();
        var firstRegistrationCount = services.Count(descriptor => descriptor.ServiceType == typeof(IFirstContract));
        var secondRegistrationCount = services.Count(descriptor => descriptor.ServiceType == typeof(ISecondContract));
        var disposableRegistrationCount = services.Count(descriptor => descriptor.ServiceType == typeof(IDisposable));
        var first = provider.GetService<IFirstContract>();
        var second = provider.GetService<ISecondContract>();
        var disposable = provider.GetService<IDisposable>();

        await Assert.That(firstRegistrationCount).IsEqualTo(1);
        await Assert.That(secondRegistrationCount).IsEqualTo(1);
        await Assert.That(disposableRegistrationCount).IsEqualTo(0);
        await Assert.That(first).IsNotNull();
        await Assert.That(second).IsNotNull();
        await Assert.That(disposable).IsNull();
    }

    /// <summary>
    ///     Verifies that resolving two different interfaces from the same
    ///     <see cref="ServiceCollectionExtensions.AddSingletonAsImplementedInterfaces{TImplementation}" />
    ///     registration returns the same shared singleton instance.
    /// </summary>
    [Test]
    public async Task AddSingletonAsImplementedInterfaces_WhenResolvingMultipleInterfaces_ReturnsSameInstance()
    {
        var services = new ServiceCollection();
        services.AddSingletonAsImplementedInterfaces<SampleImplementation>(CreateSampleImplementation);

        using ServiceProvider provider = services.BuildServiceProvider();
        var first = provider.GetService<IFirstContract>();
        var second = provider.GetService<ISecondContract>();

        await Assert.That(first).IsSameReferenceAs(second);
    }

    /// <summary>
    ///     Resolving the singletons that are wired in by AddProxyListener forces the
    ///     DI factory lambdas to execute, covering the body of those registrations
    ///     (HttpClient, IComposerHistoryStore, IUserPreferencesStore, RemoteDeviceTracker,
    ///     IReverseProxyEngine alias).
    /// </summary>
    [Test]
    public async Task AddProxyListener_ResolvingFactoryRegistrations_ExercisesFactoryLambdas()
    {
        var services = new ServiceCollection();
        var configurationBuilder = new ConfigurationBuilder();
        var configuration = configurationBuilder.Build();
        services.AddLogging();
        services.AddSingleton<IDomainEventBus, DomainEventBus>();
        services.AddProxyListener(configuration);

        ServiceProvider provider = services.BuildServiceProvider();
        try
        {
            var httpClient = provider.GetService<System.Net.Http.HttpClient>();
            var historyStore = provider.GetService<Proxyfan.Domain.Traffic.IComposerHistoryStore>();
            var preferencesStore = provider.GetService<Proxyfan.Domain.Configuration.IUserPreferencesStore>();
            var remoteDeviceTracker = provider.GetService<Proxyfan.Domain.RemoteDevices.RemoteDeviceTracker>();
            var reverseProxyEngine = provider.GetService<Proxyfan.Domain.Proxy.IReverseProxyEngine>();

            await Assert.That(httpClient).IsNotNull();
            await Assert.That(historyStore).IsNotNull();
            await Assert.That(preferencesStore).IsNotNull();
            await Assert.That(remoteDeviceTracker).IsNotNull();
            await Assert.That(reverseProxyEngine).IsNotNull();
        }
        finally
        {
            await provider.DisposeAsync();
        }
    }

    /// <summary>
    ///     Resolving the auto-update, plugin, and reverse-proxy health-checker singletons
    ///     forces the matching factory lambdas to execute, covering the
    ///     <c>&lt;AddAutoUpdate&gt;b__*</c>, <c>&lt;AddPlugins&gt;b__*</c>, and
    ///     <c>&lt;AddReverseProxy&gt;b__*</c> closures.
    /// </summary>
    [Test]
    public async Task AddProxyListener_ResolvingUpdatePluginReverseProxyFactories_ExercisesLambdas()
    {
        var services = new ServiceCollection();
        var configurationBuilder = new ConfigurationBuilder();
        var configuration = configurationBuilder.Build();
        services.AddLogging();
        services.AddSingleton<IDomainEventBus, DomainEventBus>();
        services.AddProxyListener(configuration);

        ServiceProvider provider = services.BuildServiceProvider();
        try
        {
            var updateFeedFunction = provider.GetService<Proxyfan.Domain.Updates.UpdateFeedFunction>();
            var updateChecker = provider.GetService<Proxyfan.Domain.Updates.IUpdateChecker>();
            var enabledStateStore = provider.GetService<Proxyfan.Plugin.Abstractions.IPluginEnabledStateStore>();
            var rootProvider = provider.GetService<Proxyfan.Framework.Extensibility.PluginRootDirectoryProvider>();
            var pluginHost = provider.GetService<Proxyfan.Plugin.Abstractions.IPluginHost>();
            var pluginUpdateManifestUrlProvider = provider.GetService<Proxyfan.Framework.Extensibility.PluginUpdateManifestUrlProvider>();
            var healthChecker = provider.GetService<Proxyfan.Domain.Proxy.PeriodicReverseProxyHealthChecker>();

            await Assert.That(updateFeedFunction).IsNotNull();
            await Assert.That(updateChecker).IsNotNull();
            await Assert.That(enabledStateStore).IsNotNull();
            await Assert.That(rootProvider).IsNotNull();
            await Assert.That(pluginHost).IsNotNull();
            await Assert.That(pluginUpdateManifestUrlProvider).IsNotNull();
            await Assert.That(healthChecker).IsNotNull();
        }
        finally
        {
            await provider.DisposeAsync();
        }
    }

    /// <summary>
    ///     Verifies that <see cref="ServiceCollectionExtensions.StartClientBackgroundServices" />
    ///     activates plugins via the composition-layer startup hook.
    /// </summary>
    [Test]
    public async Task StartClientBackgroundServices_WhenCalled_ActivatesPlugins()
    {
        var services = new ServiceCollection();
        var configurationBuilder = new ConfigurationBuilder();
        var configuration = configurationBuilder.Build();
        services.AddLogging();
        services.AddSingleton<IDomainEventBus, DomainEventBus>();
        services.AddProxyListener(configuration);

        ServiceProvider provider = services.BuildServiceProvider();
        try
        {
            provider.StartClientBackgroundServices();
            var activationService = provider.GetRequiredService<IPluginActivationService>();
            await Assert.That(activationService.IsActivated).IsTrue();
        }
        finally
        {
            await provider.DisposeAsync();
        }
    }

    private static ServiceProvider CreateProvider(
        IBreakpointHandler? breakpointHandler = null,
        IScriptingHandler? scriptingHandler = null)
    {
        var services = new ServiceCollection();
        var configurationBuilder = new ConfigurationBuilder();
        var configuration = configurationBuilder.Build();
        services.AddLogging();
        services.AddSingleton<IDomainEventBus, DomainEventBus>();
        services.AddProxyListener(configuration);
        if (breakpointHandler is not null)
        {
            services.AddSingleton(breakpointHandler);
            services.AddSingleton<IBreakpointHandler>(breakpointHandler);
        }

        if (scriptingHandler is not null)
        {
            services.AddSingleton(scriptingHandler);
            services.AddSingleton<IScriptingHandler>(scriptingHandler);
        }

        return services.BuildServiceProvider();
    }

    private static HypertextTransferProtocolRequestData CreateRequest(string url, HeaderCollection? headers = null)
    {
        var requestHeaders = headers ?? HeaderCollection.Empty.Add("Host", new Uri(url).Authority);
        var parameters = new HypertextTransferProtocolRequestDataParameters
        {
            Body = Array.Empty<byte>(),
            Headers = requestHeaders,
            Method = "GET",
            RequestUri = new Uri(url),
            Version = "HTTP/1.1",
        };
        return new HypertextTransferProtocolRequestData(parameters);
    }

    private static HypertextTransferProtocolRequestData AddRequestHeader(
        HypertextTransferProtocolRequestData request,
        string name,
        string value)
    {
        var parameters = new HypertextTransferProtocolRequestDataParameters
        {
            Body = request.Body,
            Headers = request.Headers.Add(name, value),
            Method = request.Method,
            RequestUri = request.RequestUri,
            Version = request.Version,
        };
        return new HypertextTransferProtocolRequestData(parameters);
    }

    private static HypertextTransferProtocolResponseData CreateResponse(int statusCode, HeaderCollection? headers = null)
    {
        var parameters = new HypertextTransferProtocolResponseDataParameters
        {
            Body = Array.Empty<byte>(),
            Headers = headers ?? HeaderCollection.Empty,
            ReasonPhrase = "OK",
            StatusCode = statusCode,
            Version = "HTTP/1.1",
        };
        return new HypertextTransferProtocolResponseData(parameters);
    }

    private static HypertextTransferProtocolResponseData AddResponseHeader(
        HypertextTransferProtocolResponseData response,
        string name,
        string value)
    {
        var parameters = new HypertextTransferProtocolResponseDataParameters
        {
            Body = response.Body,
            Headers = response.Headers.Add(name, value),
            ReasonPhrase = response.ReasonPhrase,
            StatusCode = response.StatusCode,
            Version = response.Version,
        };
        return new HypertextTransferProtocolResponseData(parameters);
    }

    private static SampleImplementation CreateSampleImplementation()
    {
        var implementation = new SampleImplementation();
        return implementation;
    }

    private interface IFirstContract
    {
    }

    private interface ISecondContract
    {
    }

    private sealed class SampleImplementation : IFirstContract, ISecondContract, IDisposable
    {
        public void Dispose()
        {
        }
    }

    private sealed class StubBreakpointHandler : IBreakpointHandler
    {
        public string? RequestHeaderName { get; init; }

        public string? RequestHeaderValue { get; init; }

        public string? ResponseHeaderName { get; init; }

        public string? ResponseHeaderValue { get; init; }

        public List<HypertextTransferProtocolRequestData> SeenRequests { get; } = [];

        public List<HypertextTransferProtocolResponseData> SeenResponses { get; } = [];

        public Task<BreakpointDecision> ResolveRequestAsync(
            HypertextTransferProtocolRequestData request,
            CancellationToken cancellationToken)
        {
            SeenRequests.Add(request);
            if (RequestHeaderName is null || RequestHeaderValue is null)
            {
                return Task.FromResult(BreakpointDecisions.ResumeRequest(request));
            }

            var modifiedRequest = AddRequestHeader(request, RequestHeaderName, RequestHeaderValue);
            return Task.FromResult(BreakpointDecisions.ResumeRequest(modifiedRequest));
        }

        public Task<BreakpointDecision> ResolveResponseAsync(
            HypertextTransferProtocolRequestData request,
            HypertextTransferProtocolResponseData response,
            CancellationToken cancellationToken)
        {
            SeenResponses.Add(response);
            if (ResponseHeaderName is null || ResponseHeaderValue is null)
            {
                return Task.FromResult(BreakpointDecisions.ResumeResponse(response));
            }

            var modifiedResponse = AddResponseHeader(response, ResponseHeaderName, ResponseHeaderValue);
            return Task.FromResult(BreakpointDecisions.ResumeResponse(modifiedResponse));
        }
    }

    private sealed class StubScriptingHandler : IScriptingHandler
    {
        public string? RequestHeaderName { get; init; }

        public string? RequestHeaderValue { get; init; }

        public string? ResponseHeaderName { get; init; }

        public string? ResponseHeaderValue { get; init; }

        public List<HypertextTransferProtocolRequestData> SeenRequests { get; } = [];

        public List<HypertextTransferProtocolResponseData> SeenResponses { get; } = [];

        public Task<Result<HypertextTransferProtocolRequestData>> ApplyRequestAsync(
            string flowId,
            HypertextTransferProtocolRequestData request,
            CancellationToken cancellationToken)
        {
            SeenRequests.Add(request);
            if (RequestHeaderName is null || RequestHeaderValue is null)
            {
                return Task.FromResult(Result.Success(request));
            }

            var modifiedRequest = AddRequestHeader(request, RequestHeaderName, RequestHeaderValue);
            return Task.FromResult(Result.Success(modifiedRequest));
        }

        public Task<Result<HypertextTransferProtocolResponseData>> ApplyResponseAsync(
            string flowId,
            HypertextTransferProtocolRequestData request,
            HypertextTransferProtocolResponseData response,
            CancellationToken cancellationToken)
        {
            SeenResponses.Add(response);
            if (ResponseHeaderName is null || ResponseHeaderValue is null)
            {
                return Task.FromResult(Result.Success(response));
            }

            var modifiedResponse = AddResponseHeader(response, ResponseHeaderName, ResponseHeaderValue);
            return Task.FromResult(Result.Success(modifiedResponse));
        }
    }
}