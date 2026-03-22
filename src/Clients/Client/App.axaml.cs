using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using Avalonia.Diagnostics;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Proxyfan.Client.Shell.ViewModels;
using Proxyfan.Client.Shell.Views;
using Proxyfan.DependencyInjection;
using Proxyfan.Presentation;
using IApplicationLifetime = Avalonia.Controls.ApplicationLifetimes.IApplicationLifetime;

namespace Proxyfan.Client;

/// <summary>The Avalonia application entry point for the multi-platform client.</summary>
[SuppressMessage("ReSharper", "PartialTypeWithSinglePart")]
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
        _hostBuilder.ConfigureServices(services =>
        {
            services.AddSingletonAsImplementedInterfaces(ResolveApplicationLifetime);
            services.AddSingleton<ShellViewModel>();
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
            // Avoid duplicate validations from both Avalonia and the CommunityToolkit.
            // More info: https://docs.avaloniaui.net/docs/guides/development-guides/data-validation#manage-validationplugins

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

    private static ShellWindow CreateShellWindow()
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
                this.AttachDevTools(new DevToolsOptions());
            }
#endif

            ContainerLocator.Set(() => host.Services);
            host.Start();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Fatal exception during host initialization: {ex}");
            host?.Stop();
            throw;
        }
    }
}