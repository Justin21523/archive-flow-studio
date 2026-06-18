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

    public async override void OnFrameworkInitializationCompleted()
    {
        // DB columns are snake_case (file_name, file_extension, ...) while entities are PascalCase.
        // Enable Dapper underscore matching so `SELECT *` maps correctly (otherwise FileName/FileExtension come back empty).
        Dapper.DefaultTypeMap.MatchNamesWithUnderscores = true;

        var services = new ServiceCollection();
        ConfigureServices(services);
        Services = services.BuildServiceProvider();

        var dbInitializer = Services.GetRequiredService<IDatabaseInitializer>();
        dbInitializer.Initialize();
        
        // Register all built-in nodes into the registry
        var nodeRegistry = Services.GetRequiredService<ArchiveFlow.Application.Services.NodeRegistry>();
        ArchiveFlow.Application.Services.BuiltInNodeRegistrar.RegisterAll(nodeRegistry, Services);

        var pluginManager = Services.GetRequiredService<ArchiveFlow.Infrastructure.Services.PluginManager>();
        pluginManager.LoadPlugins();
        // Initialize Mock Data if database is empty
        var mockDataService = Services.GetRequiredService<ArchiveFlow.Application.Interfaces.IMockDataService>();
        await mockDataService.GenerateMockDataAsync();
        
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
        services.AddSingleton<ArchiveFlow.Application.Interfaces.IMetadataRepository, ArchiveFlow.Infrastructure.Database.Repositories.SqliteMetadataRepository>();
        services.AddSingleton<ArchiveFlow.Application.Interfaces.ISearchService, ArchiveFlow.Infrastructure.Search.SqliteSearchService>();
        services.AddSingleton<ArchiveFlow.Application.Interfaces.IWorkflowStorageService, ArchiveFlow.Infrastructure.Storage.LocalWorkflowStorageService>();
        services.AddSingleton<ArchiveFlow.Application.Interfaces.IFilePreviewService, ArchiveFlow.Infrastructure.Preview.FilePreviewService>();
        services.AddSingleton<ArchiveFlow.Application.Interfaces.IMockDataService, ArchiveFlow.Infrastructure.Services.MockDataService>();
        services.AddSingleton<ArchiveFlow.Application.Interfaces.IAutoTaggingService, ArchiveFlow.Infrastructure.Services.LocalKeywordTaggingService>();
        // Register the background batch job service as a singleton
        services.AddSingleton<ArchiveFlow.Application.Interfaces.IBatchJobService, ArchiveFlow.Infrastructure.Services.BackgroundBatchJobService>();
        services.AddSingleton<ArchiveFlow.Application.Services.NodeRegistry>();
        services.AddSingleton<ArchiveFlow.Infrastructure.Services.PluginManager>();
        services.AddTransient<ArchiveFlow.App.ViewModels.DatabaseManagerViewModel>();
        // Register Relationship Repository
        services.AddSingleton<ArchiveFlow.Application.Interfaces.IRelationshipRepository, ArchiveFlow.Infrastructure.Database.Repositories.SqliteRelationshipRepository>();
        // Register Graph Explorer ViewModel
        services.AddTransient<ArchiveFlow.App.ViewModels.GraphExplorerViewModel>();
        services.AddSingleton<ArchiveFlow.Application.Interfaces.IDublinCoreExportService, ArchiveFlow.Infrastructure.Export.DublinCoreExportService>();
        services.AddSingleton<ArchiveFlow.Application.Interfaces.IStatisticsService, ArchiveFlow.Infrastructure.Services.SqliteStatisticsService>();
        services.AddTransient<ArchiveFlow.App.ViewModels.DashboardViewModel>();
        
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
        var dbPath = System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), "Data", "archiveflow.db");
        System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(dbPath)!);
        return $"Data Source={dbPath};";
    }
}
