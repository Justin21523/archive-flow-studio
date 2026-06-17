using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using ArchiveFlow.App.ViewModels;
using ArchiveFlow.App.Views;
using Microsoft.Extensions.DependencyInjection;
using ArchiveFlow.Application.Interfaces;
using ArchiveFlow.Infrastructure.Database;
using ArchiveFlow.Infrastructure.Database.Repositories;
using ArchiveFlow.Infrastructure.FileSystem;
using ArchiveFlow.Infrastructure.Hashing;
using FluentMigrator.Runner;
using Microsoft.Extensions.Logging;
using System;

namespace ArchiveFlow.App;

public partial class App : Avalonia.Application // <--- 這裡加上 Avalonia. 解決衝突
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

        var dbInitializer = Services.GetRequiredService<IDatabaseInitializer>();
        dbInitializer.Initialize();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow
            {
                DataContext = Services.GetRequiredService<NodeCanvasViewModel>(),
            };
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void ConfigureServices(IServiceCollection services)
    {
        services.AddLogging(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Debug);
        });

        services.AddSingleton<IFileHashingService, Sha256FileHashingService>();
        services.AddTransient<IFileScanner, LocalFileScanner>();
        services.AddSingleton<IFileRepository, SqliteFileRepository>();
        services.AddTransient<ArchiveFlow.Application.Workflows.WorkflowEngine>();
        services.AddTransient<ArchiveFlow.App.ViewModels.NodeCanvasViewModel>();

        services.AddFluentMigratorCore()
            .ConfigureRunner(rb => rb
                .AddSQLite()
                .WithGlobalConnectionString(GetConnectionString())
                .ScanIn(typeof(DatabaseInitializer).Assembly).For.Migrations());
        
        services.AddSingleton<IDatabaseInitializer, DatabaseInitializer>();
        services.AddTransient<MainWindowViewModel>();
    }

    private static string GetConnectionString()
    {
        var dbPath = System.IO.Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ArchiveFlow",
            "archiveflow.db"
        );
        System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(dbPath)!);
        return $"Data Source={dbPath};";
    }
}
