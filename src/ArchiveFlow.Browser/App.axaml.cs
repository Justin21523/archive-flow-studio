using ArchiveFlow.Application.Interfaces;
using ArchiveFlow.Browser.Services;
using ArchiveFlow.Browser.ViewModels;
using ArchiveFlow.Browser.Views;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace ArchiveFlow.Browser;

public partial class App : Avalonia.Application
{
    public static IServiceProvider Services { get; private set; } = null!;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        var services = new ServiceCollection();
        ConfigureServices(services);

        Services = services.BuildServiceProvider();

        var dataService = Services.GetRequiredService<IDemoDataService>();
        dataService.ResetAsync().GetAwaiter().GetResult();

        if (ApplicationLifetime is ISingleViewApplicationLifetime singleView)
        {
            singleView.MainView = new MainView
            {
                DataContext = Services.GetRequiredService<BrowserDemoViewModel>()
            };
        }
        else if (ApplicationLifetime is IActivityApplicationLifetime activity)
        {
            activity.MainViewFactory = () => new MainView
            {
                DataContext = Services.GetRequiredService<BrowserDemoViewModel>()
            };
        }

        base.OnFrameworkInitializationCompleted();
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(NullLoggerFactory.Instance);
        services.AddSingleton<BrowserDemoDataStore>();
        services.AddSingleton<IDataRepository>(provider => provider.GetRequiredService<BrowserDemoDataStore>());
        services.AddSingleton<IFileRepository>(provider => provider.GetRequiredService<BrowserDemoDataStore>());
        services.AddSingleton<IMetadataRepository>(provider => provider.GetRequiredService<BrowserDemoDataStore>());
        services.AddSingleton<IRelationshipRepository>(provider => provider.GetRequiredService<BrowserDemoDataStore>());
        services.AddSingleton<IExportJobRepository>(provider => provider.GetRequiredService<BrowserDemoDataStore>());
        services.AddSingleton<IImportJobRepository>(provider => provider.GetRequiredService<BrowserDemoDataStore>());
        services.AddSingleton<IDemoDataService>(provider => provider.GetRequiredService<BrowserDemoDataStore>());
        services.AddSingleton<IImportPipelineService>(provider => provider.GetRequiredService<BrowserDemoDataStore>());
        services.AddSingleton<IExportService>(provider => provider.GetRequiredService<BrowserDemoDataStore>());
        services.AddSingleton<IAppEnvironmentService, BrowserAppEnvironmentService>();
        services.AddSingleton<IStorageService, BrowserMemoryStorageService>();
        services.AddSingleton<IFilePickerService, BrowserDemoFilePickerService>();
        services.AddSingleton<INotificationService, BrowserDemoNotificationService>();
        services.AddTransient<BrowserDemoViewModel>();
    }
}
