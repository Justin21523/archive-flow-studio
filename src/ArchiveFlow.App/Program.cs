using System;
using Avalonia;

namespace ArchiveFlow.App;

sealed class Program
{
    [STAThread]
    public static void Main(string[] args) => BuildAvaloniaApp()
        .StartWithClassicDesktopLifetime(args);

    public static AppBuilder BuildAvaloniaApp()
        // Use fully qualified name to avoid namespace conflict
        => AppBuilder.Configure<ArchiveFlow.App.App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
}
