using System;
using Avalonia;

namespace Proxyfan.Client.Desktop;

/// <summary>Entry point for the client application.</summary>
public static class Program
{
    /// <summary>Application entry point. Builds the Avalonia app and starts the classic desktop lifetime.</summary>
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