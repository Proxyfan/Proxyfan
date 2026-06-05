using Avalonia;
using System;

namespace Proxyfan.Client.Desktop;

/// <summary>
///     Entry point for the client application.
/// </summary>
public static class Program
{
    /// <summary>
    ///     Builds the Avalonia app builder used by the desktop entry point.
    /// </summary>
    /// <returns>The configured <see cref="AppBuilder" />.</returns>
    public static AppBuilder BuildAppBuilder()
    {
        return AppBuilder.Configure<App>()
                         .UsePlatformDetect()
                         .LogToTrace()
                         .WithInterFont();
    }

    /// <summary>
    ///     Application entry point. Builds the Avalonia app and starts the classic desktop lifetime.
    /// </summary>
    /// <param name="args">Command-line arguments.</param>
    [STAThread]
    public static void Main(string[] args)
    {
        Run(StartClassicDesktopLifetime, args);
    }

    /// <summary>
    ///     Runs the desktop application with the provided lifetime starter.
    /// </summary>
    /// <param name="desktopLifetimeStarter">The desktop lifetime starter to invoke.</param>
    /// <param name="args">Command-line arguments.</param>
    public static void Run(DesktopLifetimeStarter? desktopLifetimeStarter, string[] args)
    {
        ArgumentNullException.ThrowIfNull(desktopLifetimeStarter);
        var appBuilder = BuildAppBuilder();
        desktopLifetimeStarter(appBuilder, args);
    }

    private static void StartClassicDesktopLifetime(AppBuilder appBuilder, string[] args)
    {
        appBuilder.StartWithClassicDesktopLifetime(args);
    }

    /// <summary>
    ///     Starts the configured classic desktop lifetime.
    /// </summary>
    /// <param name="appBuilder">The configured Avalonia app builder.</param>
    /// <param name="args">Command-line arguments.</param>
    public delegate void DesktopLifetimeStarter(AppBuilder appBuilder, string[] args);
}