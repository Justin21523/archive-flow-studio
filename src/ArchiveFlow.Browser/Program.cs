using Avalonia;
using Avalonia.Browser;
using ArchiveFlow.Browser;

internal sealed partial class Program
{
    private static Task Main(string[] args)
    {
        return BuildAvaloniaApp()
            .WithInterFont()
            .StartBrowserAppAsync("out");
    }

    public static AppBuilder BuildAvaloniaApp()
    {
        return AppBuilder.Configure<App>();
    }
}
