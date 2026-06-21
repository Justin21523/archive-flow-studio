using System;
using ArchiveFlow.App.ViewModels;
using ArchiveFlow.App.Views;
using ArchiveFlow.Application.Interfaces;
using ArchiveFlow.Application.Services;
using ArchiveFlow.Infrastructure.Database;
using ArchiveFlow.Infrastructure.Database.Repositories;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using FluentMigrator.Runner;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ArchiveFlow.App;

public partial class App : Avalonia.Application
{
    public static IServiceProvider Services { get; private set; } = null!;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        var serviceCollection = new ServiceCollection();
        ConfigureServices(serviceCollection);

        Services = serviceCollection.BuildServiceProvider();

        var databaseInitializer = Services.GetRequiredService<IDatabaseInitializer>();
        databaseInitializer.Initialize();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow
            {
                DataContext = Services.GetRequiredService<MainWindowViewModel>()
            };
        }

        base.OnFrameworkInitializationCompleted();
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        services.AddLogging(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Information);
        });

        services.AddSingleton<IDatabaseConnectionFactory, SqliteConnectionFactory>();

        services.AddFluentMigratorCore()
            .ConfigureRunner(builder =>
            {
                var factory = new SqliteConnectionFactory();

                builder
                    .AddSQLite()
                    .WithGlobalConnectionString(factory.ConnectionString)
                    .ScanIn(typeof(DatabaseInitializer).Assembly)
                    .For.Migrations();
            })
            .AddLogging(builder => builder.AddFluentMigratorConsole());

        services.AddSingleton<IDatabaseInitializer, DatabaseInitializer>();
        services.AddSingleton<IFileRepository, SqliteFileRepository>();

        services.AddSingleton<NodeRegistry>();

        services.AddTransient<NodeCanvasViewModel>();
        services.AddTransient<MainWindowViewModel>();
    }
}