using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Proxyfan.Domain;
using Proxyfan.Domain.Certificates;
using Proxyfan.Domain.Configuration;
using Proxyfan.Domain.DomainNameSystemSpoofing;
using Proxyfan.Domain.Proxy;
using Proxyfan.Domain.RemoteDevices;
using Proxyfan.Domain.Rules;
using Proxyfan.Domain.Rules.Rules;
using Proxyfan.Domain.Scripting;
using Proxyfan.Domain.Throttling;
using Proxyfan.Domain.Traffic;
using Proxyfan.Domain.Traffic.Diff;
using Proxyfan.Domain.Updates;
using Proxyfan.Framework.Extensibility;
using Proxyfan.Framework.Networking;
using Proxyfan.Framework.Platform;
using Proxyfan.Framework.Serialization;
using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Runtime.Versioning;

namespace Proxyfan.DependencyInjection;

/// <summary>
///     Extension methods for <see cref="Microsoft.Extensions.DependencyInjection.IServiceCollection" />
///     that register types against every interface they implement.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    ///     Registers the proxy listener services, including <see cref="IProxyListener" />, <see cref="ProxyOptions" />
    ///     binding, and options validation.
    ///     <para>
    ///         <strong>Prerequisite:</strong> An <see cref="Proxyfan.Domain.IDomainEventBus" /> singleton must already be
    ///         registered in <paramref name="serviceCollection" /> before calling this method.
    ///     </para>
    /// </summary>
    /// <param name="serviceCollection">The service collection to register services into.</param>
    /// <param name="configuration">The configuration used to bind <see cref="ProxyOptions" />.</param>
    /// <returns>The service collection, for chaining.</returns>
    [SupportedOSPlatform("windows")]
    public static IServiceCollection AddProxyListener(
        this IServiceCollection serviceCollection,
        IConfiguration configuration)
    {
        serviceCollection.Configure<ProxyOptions>(configuration.GetSection(ProxyOptions.SectionKey));
        serviceCollection.AddSingleton<IValidateOptions<ProxyOptions>, ProxyOptionsValidator>();
        serviceCollection.AddSingleton<ITrafficStore, TrafficStore>();
        serviceCollection.AddSingleton<IWebSocketStore, WebSocketStore>();
        serviceCollection.AddSingleton<IServerSentEventsStore, ServerSentEventsStore>();
        serviceCollection.AddSingleton<IRemoteProcedureCallStore, RemoteProcedureCallStore>();
        serviceCollection.AddSingleton<TrafficFlowDiffPool>();
        serviceCollection.AddSingleton<ICertificateGenerator, RsaCertificateGenerator>();
        serviceCollection.AddSingleton<ICertificateStore, WindowsCertificateStore>();
        serviceCollection.AddSingleton<MutableCertificateAuthorityProvider>();
        var serverNameIndicationProxyingList = new ServerNameIndicationProxyingList(isEnabled: true);
        serverNameIndicationProxyingList.AddIncludedPattern("*");
        serviceCollection.AddSingleton(serverNameIndicationProxyingList);
        serviceCollection.AddSingleton<TransportLayerSecurityInterceptionContext>();
        serviceCollection.AddSingleton<TransportLayerSecurityInterceptorHandlerDependencies>(BuildTransportLayerSecurityDependencies);
        serviceCollection.AddSingleton<HypertextTransferProtocolProxyHandlerDependencies>(BuildHypertextTransferProtocolDependencies);
        serviceCollection.AddSingleton<IConnectionHandler, HypertextTransferProtocolProxyHandler>();
        serviceCollection.AddSingleton<IConnectionHandler, TransportLayerSecurityInterceptorHandler>();
        serviceCollection.AddSingleton<IConnectionHandler, SocksTunnelHandler>();
        serviceCollection.AddSingleton<IProxyListener, SocketProxyListener>();
        serviceCollection.AddSingleton<ISystemProxy, WindowsSystemProxy>();
        serviceCollection.AddSingleton<IConnectionDispatcher, ConnectionDispatcher>();
        serviceCollection.AddSingleton<MutableThrottleProfile>();
        serviceCollection.AddSingleton<DomainNameSystemOverrideMap>();
        serviceCollection.AddSingleton<UpstreamHostResolver>();
        serviceCollection.AddSingleton(TimeProvider.System);
        AddRuleEngine(serviceCollection);
        AddScripting(serviceCollection);
        AddComposer(serviceCollection);
        AddPlugins(serviceCollection);
        AddPreferences(serviceCollection);
        AddRemoteDevices(serviceCollection);
        AddReverseProxy(serviceCollection);
        var entryAssembly = Assembly.GetEntryAssembly();
        var entryAssemblyVersion = entryAssembly?.GetName().Version;
        var currentVersion = entryAssemblyVersion is null ? "0.0.0" : entryAssemblyVersion.ToString(3);
        AddAutoUpdate(serviceCollection, currentVersion);
        return serviceCollection;
    }

    /// <summary>
    ///     Registers <paramref name="implementation" /> as a singleton against every interface it implements,
    ///     excluding <see cref="IDisposable" />.
    /// </summary>
    /// <typeparam name="TImplementation">The concrete type of the implementation instance.</typeparam>
    /// <param name="serviceCollection">The service collection to register services into.</param>
    /// <param name="implementation">The factory that produces the singleton instance.</param>
    [RequiresUnreferencedCode("Scans implemented interfaces by reflection; not trim-safe by design.")]
    public static void AddSingletonAsImplementedInterfaces<TImplementation>(
        this IServiceCollection serviceCollection,
        ImplementationFactory<TImplementation> implementation)
        where TImplementation : notnull
    {
        var type = typeof(TImplementation);

        foreach (var @interface in type.GetInterfaces())
        {
            if (@interface != typeof(IDisposable))
            {
                var descriptor = new ServiceDescriptor(@interface, _ => implementation.Invoke(), ServiceLifetime.Singleton);
                serviceCollection.Add(descriptor);
            }
        }
    }

    private static void AddAutoUpdate(IServiceCollection serviceCollection, string currentVersion)
    {
        const string defaultOwner = "Proxyfan";
        const string defaultRepository = "Proxyfan";
        var defaultInterval = TimeSpan.FromHours(24);
        var defaultInitialDelay = TimeSpan.FromMinutes(1);

        var notification = new MutableUpdateNotification();
        serviceCollection.AddSingleton(notification);
        serviceCollection.AddSingleton<UpdateFeedFunction>(static provider =>
        {
            var hypertextTransferProtocolClient = provider.GetRequiredService<HttpClient>();
            return GitHubReleasesUpdateFeed.Create(hypertextTransferProtocolClient, defaultOwner, defaultRepository);
        });
        serviceCollection.AddSingleton<IUpdateChecker>(static provider =>
        {
            var feed = provider.GetRequiredService<UpdateFeedFunction>();
            var checker = new UpdateChecker(feed);
            return checker;
        });
        var options = new PeriodicUpdateCheckOptions
        {
            CurrentVersion = currentVersion,
            InitialDelay = defaultInitialDelay,
            PollInterval = defaultInterval,
        };
        serviceCollection.AddSingleton(options);
        serviceCollection.AddSingleton<PeriodicUpdateChecker>();
    }

    private static void AddComposer(IServiceCollection serviceCollection)
    {
        serviceCollection.AddSingleton<HttpClient>(static _ =>
        {
            var handler = new HttpClientHandler
            {
                AllowAutoRedirect = false,
                UseProxy = false,
            };
            var client = new HttpClient(handler, disposeHandler: true);
            return client;
        });
        serviceCollection.AddSingleton<IComposerRequestSender, ComposerRequestSender>();
        serviceCollection.AddSingleton<IRequestRepeater, RequestRepeater>();
        serviceCollection.AddSingleton<IComposerHistoryStore>(static _ =>
        {
            var directory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Proxyfan");
            Directory.CreateDirectory(directory);
            var path = Path.Combine(directory, "composer-history.json");
            return new FileComposerHistoryStore(path);
        });
        serviceCollection.AddSingleton<ComposerHistoryService>();
    }

    [SupportedOSPlatform("windows")]
    private static void AddPlugins(IServiceCollection serviceCollection)
    {
        serviceCollection.AddSingleton<PluginRegistry>();
        serviceCollection.AddSingleton<PluginDirectoryScanner>();
        serviceCollection.AddSingleton<IPluginInstanceFactory, IsolatedPluginInstanceFactory>();
        serviceCollection.AddSingleton<IPluginEnabledStateStore>(static _ =>
        {
            var directory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Proxyfan",
                "plugins");
            Directory.CreateDirectory(directory);
            var path = Path.Combine(directory, "disabled-plugins.txt");
            return new FilePluginEnabledStateStore(path);
        });
        serviceCollection.AddSingleton<PluginLoader>();
        serviceCollection.AddSingleton<PluginRootDirectoryProvider>(static _ =>
        {
            var directory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Proxyfan",
                "plugins");
            Directory.CreateDirectory(directory);
            var provider = new PluginRootDirectoryProvider(directory);
            return provider;
        });
        serviceCollection.AddSingleton<DefaultPluginHost>();
        serviceCollection.AddSingleton<Proxyfan.Plugin.Abstractions.IPluginHost>(static serviceProvider => serviceProvider.GetRequiredService<DefaultPluginHost>());
        serviceCollection.AddSingleton<PluginActivationService>();
        serviceCollection.AddSingleton<IPluginFolderOpener, WindowsPluginFolderOpener>();
        serviceCollection.AddSingleton<IPluginDirectoryWatcher, FileSystemPluginDirectoryWatcher>();
        serviceCollection.AddSingleton<PluginUpdateManifestUrlProvider>(static _ =>
        {
            var provider = new PluginUpdateManifestUrlProvider(string.Empty);
            return provider;
        });
        serviceCollection.AddSingleton<IPluginUpdateFeed, HypertextTransferProtocolPluginUpdateFeed>();
    }

    private static void AddPreferences(IServiceCollection serviceCollection)
    {
        serviceCollection.AddSingleton<IUserPreferencesStore>(static _ =>
        {
            var directory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Proxyfan");
            Directory.CreateDirectory(directory);
            var path = Path.Combine(directory, "preferences.json");
            return new FileUserPreferencesStore(path);
        });
    }

    private static void AddRemoteDevices(IServiceCollection serviceCollection)
    {
        serviceCollection.AddSingleton<RemoteDeviceTracker>(static serviceProvider =>
        {
            var timeProvider = serviceProvider.GetRequiredService<TimeProvider>();
            var tracker = new RemoteDeviceTracker(timeProvider);
            return tracker;
        });
        serviceCollection.AddSingleton<RemoteDeviceTrackerEventBridge>();
    }

    private static void AddReverseProxy(IServiceCollection serviceCollection)
    {
        serviceCollection.AddSingleton<ReverseProxyRouteRegistry>();
        serviceCollection.AddSingleton<IBackendHealthProbe, TransportControlProtocolBackendHealthProbe>();
        serviceCollection.AddSingleton(serviceProvider =>
        {
            var dependencies = new ReverseProxyHypertextTransferProtocolHandlerDependencies
            {
                EventBus = serviceProvider.GetRequiredService<IDomainEventBus>(),
                Logger = serviceProvider.GetRequiredService<ILogger<ReverseProxyHypertextTransferProtocolHandler>>(),
                RuleEngine = serviceProvider.GetRequiredService<IRuleEngine>(),
                TimeProvider = serviceProvider.GetRequiredService<TimeProvider>(),
                TrafficStore = serviceProvider.GetRequiredService<ITrafficStore>(),
            };
            var handler = new ReverseProxyHypertextTransferProtocolHandler(dependencies);
            return handler;
        });
        serviceCollection.AddSingleton<ReverseProxyEngine>();
        serviceCollection.AddSingleton<IReverseProxyEngine>(static serviceProvider => serviceProvider.GetRequiredService<ReverseProxyEngine>());
        serviceCollection.AddSingleton(static serviceProvider =>
        {
            var engine = serviceProvider.GetRequiredService<IReverseProxyEngine>();
            var options = new PeriodicReverseProxyHealthCheckOptions
            {
                InitialDelay = TimeSpan.FromSeconds(5),
                PollInterval = TimeSpan.FromSeconds(30),
            };
            return new PeriodicReverseProxyHealthChecker(engine, options);
        });
    }

    private static void AddRuleEngine(IServiceCollection serviceCollection)
    {
        serviceCollection.AddSingleton<IRuleRegistry, RuleRegistry>();
        RegisterMutableRuleInstances(serviceCollection);
        serviceCollection.AddSingleton<IBreakpointPauseInbox, BreakpointPauseInbox>();
        serviceCollection.AddSingleton<IBreakpointHandler, InteractiveBreakpointHandler>();
        serviceCollection.AddSingleton<IRuleEngine>(BuildRuleEngine);
    }

    private static void AddScripting(IServiceCollection serviceCollection)
    {
        serviceCollection.AddSingleton(_ =>
        {
            var configuration = new MutableScriptingConfiguration(isEnabled: false);
            return configuration;
        });
        serviceCollection.AddSingleton<IUserScriptCompiler, RoslynUserScriptCompiler>();
        serviceCollection.AddSingleton<IScriptingHandler, UserScriptingHandler>();
    }

    private static HypertextTransferProtocolProxyHandlerDependencies BuildHypertextTransferProtocolDependencies(IServiceProvider provider)
    {
        var dependencies = new HypertextTransferProtocolProxyHandlerDependencies
        {
            TrafficStore = provider.GetRequiredService<ITrafficStore>(),
            EventBus = provider.GetRequiredService<Proxyfan.Domain.IDomainEventBus>(),
            HostResolver = provider.GetService<UpstreamHostResolver>(),
            RuleEngine = provider.GetRequiredService<IRuleEngine>(),
            Logger = provider.GetRequiredService<Microsoft.Extensions.Logging.ILogger<HypertextTransferProtocolProxyHandler>>(),
            UpstreamProxy = provider.GetService<IOptionsMonitor<UpstreamProxyOptions>>(),
            ThrottleProfile = provider.GetService<MutableThrottleProfile>(),
            BreakpointHandler = provider.GetService<Proxyfan.Domain.Rules.Rules.IBreakpointHandler>(),
            ScriptingHandler = provider.GetService<IScriptingHandler>(),
            CertificateAuthorityProvider = provider.GetService<MutableCertificateAuthorityProvider>(),
            WebSocketStore = provider.GetService<IWebSocketStore>(),
            ServerSentEventsStore = provider.GetService<IServerSentEventsStore>(),
            RemoteProcedureCallStore = provider.GetService<IRemoteProcedureCallStore>(),
            TimeProvider = provider.GetService<TimeProvider>(),
        };
        return dependencies;
    }

    private static IRuleEngine BuildRuleEngine(IServiceProvider provider)
    {
        var registry = provider.GetRequiredService<IRuleRegistry>();
        var allowList = provider.GetRequiredService<MutableAllowListRule>();
        var blockList = provider.GetRequiredService<MutableBlockListRule>();
        var mapRemote = provider.GetRequiredService<MutableMapRemoteRule>();
        var mapLocal = provider.GetRequiredService<MutableMapLocalRule>();
        var noCachingRule = provider.GetRequiredService<MutableNoCachingRule>();
        registry.RegisterRequestPhaseRule(allowList);
        registry.RegisterRequestPhaseRule(blockList);
        registry.RegisterRequestPhaseRule(mapRemote);
        registry.RegisterRequestPhaseRule(mapLocal);
        registry.RegisterRequestPhaseRule(noCachingRule);
        registry.RegisterResponsePhaseRule(noCachingRule);
        return new RuleEngine(registry);
    }

    private static TransportLayerSecurityInterceptorHandlerDependencies BuildTransportLayerSecurityDependencies(IServiceProvider provider)
    {
        var dependencies = new TransportLayerSecurityInterceptorHandlerDependencies
        {
            Context = provider.GetRequiredService<TransportLayerSecurityInterceptionContext>(),
            TrafficStore = provider.GetRequiredService<ITrafficStore>(),
            EventBus = provider.GetRequiredService<Proxyfan.Domain.IDomainEventBus>(),
            HostResolver = provider.GetService<UpstreamHostResolver>(),
            Logger = provider.GetRequiredService<Microsoft.Extensions.Logging.ILogger<TransportLayerSecurityInterceptorHandler>>(),
            RuleEngine = provider.GetService<IRuleEngine>(),
            BreakpointHandler = provider.GetService<Proxyfan.Domain.Rules.Rules.IBreakpointHandler>(),
            ScriptingHandler = provider.GetService<IScriptingHandler>(),
            ThrottleProfile = provider.GetService<MutableThrottleProfile>(),
            TimeProvider = provider.GetService<TimeProvider>(),
            WebSocketStore = provider.GetService<IWebSocketStore>(),
            ServerSentEventsStore = provider.GetService<IServerSentEventsStore>(),
            RemoteProcedureCallStore = provider.GetService<IRemoteProcedureCallStore>(),
        };
        return dependencies;
    }

    private static void RegisterMutableRuleInstances(IServiceCollection serviceCollection)
    {
        serviceCollection.AddSingleton(_ =>
        {
            var allowList = new MutableAllowListRule(priority: 50, isEnabled: false);
            return allowList;
        });
        serviceCollection.AddSingleton(_ =>
        {
            var blockList = new MutableBlockListRule(priority: 100, isEnabled: true);
            return blockList;
        });
        serviceCollection.AddSingleton(_ =>
        {
            var mapRemote = new MutableMapRemoteRule(priority: 200, isEnabled: true);
            return mapRemote;
        });
        serviceCollection.AddSingleton(_ =>
        {
            var mapLocal = new MutableMapLocalRule(priority: 300, isEnabled: true);
            return mapLocal;
        });
        serviceCollection.AddSingleton(_ =>
        {
            var configuration = new MutableBreakpointConfiguration(isEnabled: false);
            return configuration;
        });
        serviceCollection.AddSingleton(_ =>
        {
            var noCachingRule = new MutableNoCachingRule(priority: 400, isEnabled: false);
            return noCachingRule;
        });
    }
}
