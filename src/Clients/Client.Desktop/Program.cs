using Avalonia;
using System;

namespace Proxyfan.Client.Desktop;

/// <summary>
///     Entry point for the client application.
/// </summary>
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage(Justification = "Avalonia/host plumbing: requires UI thread/desktop integration, not unit-testable.")]
public static class Program
{
    /// <summary>
    ///     Application entry point. Builds the Avalonia app and starts the classic desktop lifetime.
    /// </summary>
    /// <param name="args">Command-line arguments.</param>
    [STAThread]
    public static void Main(string[] args)
    {
        var appBuilder = AppBuilder.Configure<App>()
                                   .UsePlatformDetect()
                                   .LogToTrace()
                                   .WithInterFont();
        appBuilder.StartWithClassicDesktopLifetime(args);
    }
}