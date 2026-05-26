using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using Avalonia.Diagnostics;
using Avalonia.Markup.Xaml;
using IApplicationLifetime = Avalonia.Controls.ApplicationLifetimes.IApplicationLifetime;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Proxyfan.Client.Inspector.ViewModels;
using Proxyfan.Client.Shell.ViewModels;
using Proxyfan.Client.Shell.Views;
using Proxyfan.Client.Traffic.ViewModels;
using Proxyfan.DependencyInjection;
using Proxyfan.Domain;
using Proxyfan.Domain.Proxy;
using Proxyfan.Presentation;
using Proxyfan.Presentation.Localization;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Resources;

namespace Proxyfan.Client;

/// <summary>
///     The Avalonia application entry point for the multi-platform client.
/// </summary>
public partial class App : Application
{
    private readonly IHostBuilder _hostBuilder;
    private ShellWindow? _window;

    /// <summary>
    ///     Initializes a new instance of the <see cref="App" /> class, setting up the host builder and building the host.
    /// </summary>
    public App()
    {
        _hostBuilder = Host.CreateDefaultBuilder();
        _hostBuilder.ConfigureServices((context, services) =>
        {
            services.AddSingletonAsImplementedInterfaces(ResolveApplicationLifetime);
            services.AddSingleton<IDomainEventBus, DomainEventBus>();
            services.AddProxyListener(context.Configuration);
            services.AddSingleton<ProxyServer>();
            services.AddSingleton<TrafficListViewModel>();
            services.AddSingleton<InspectorViewModel>();
            services.AddSingleton<ShellViewModel>();
            services.AddSingleton<LocalizationService>(static serviceProvider =>
            {
                var configuration = serviceProvider.GetRequiredService<IConfiguration>();
                var culture = LocaleResolver.Resolve(configuration["ui:locale"]);
                return new LocalizationService(culture);
            });
            return;

            static IApplicationLifetime ResolveApplicationLifetime()
            {
                return Current!.ApplicationLifetime!;
            }
        });
    }

    /// <inheritdoc />
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    /// <inheritdoc />
    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var dataValidationPluginsToRemove = new List<DataAnnotationsValidationPlugin>();
            foreach (var plugin in BindingPlugins.DataValidators)
            {
                if (plugin is DataAnnotationsValidationPlugin dataAnnotationsValidationPlugin)
                {
                    dataValidationPluginsToRemove.Add(dataAnnotationsValidationPlugin);
                }
            }

            foreach (var plugin in dataValidationPluginsToRemove)
            {
                BindingPlugins.DataValidators.Remove(plugin);
            }

            InitializeHost();
            _window = CreateShellWindow();
            desktop.MainWindow = _window;
        }

        base.OnFrameworkInitializationCompleted();
    }

    private ShellWindow CreateShellWindow()
    {
        return new ShellWindow();
    }

    private void InitializeHost()
    {
        IHost? host = null;
        try
        {
            host = _hostBuilder.Build();

#if DEBUG
            var hostEnvironment = host.Services.GetRequiredService<IHostEnvironment>();
            if (hostEnvironment.IsDevelopment())
            {
                var devToolsOptions = new DevToolsOptions();
                this.AttachDevTools(devToolsOptions);
            }
#endif

            ContainerLocator.Set(() => host.Services);
            var localizationService = host.Services.GetRequiredService<LocalizationService>();
            var resourceManager = new ResourceManager("Proxyfan.Client.Resources.Strings", typeof(App).Assembly);
            localizationService.RegisterManager(resourceManager);
            host.Start();
            _ = host.Services.GetRequiredService<ProxyServer>();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Fatal exception during host initialization: {ex}");
            host?.Stop();
            throw;
        }
    }
}