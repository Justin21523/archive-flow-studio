using FluentMigrator.Runner;
using Microsoft.Extensions.Logging;

namespace ArchiveFlow.Infrastructure.Database;

/// <summary>
/// Initializes the database and applies pending migrations.
/// </summary>
public interface IDatabaseInitializer
{
    void Initialize();
}

public sealed class DatabaseInitializer : IDatabaseInitializer
{
    private readonly IMigrationRunner _migrationRunner;
    private readonly IDatabaseConnectionFactory _connectionFactory;
    private readonly ILogger<DatabaseInitializer> _logger;

    public DatabaseInitializer(
        IMigrationRunner migrationRunner,
        IDatabaseConnectionFactory connectionFactory,
        ILogger<DatabaseInitializer> logger)
    {
        _migrationRunner = migrationRunner;
        _connectionFactory = connectionFactory;
        _logger = logger;
    }

    public void Initialize()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_connectionFactory.DatabasePath)!);

        _logger.LogInformation("Initializing database at {DatabasePath}", _connectionFactory.DatabasePath);

        try
        {
            _migrationRunner.MigrateUp();
            _logger.LogInformation("Database initialization completed.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database initialization failed.");
            throw;
        }
    }
}