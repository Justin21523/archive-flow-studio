using FluentMigrator.Runner;
using Microsoft.Extensions.Logging;

namespace ArchiveFlow.Infrastructure.Database;

public interface IDatabaseInitializer
{
    void Initialize();
}

public class DatabaseInitializer : IDatabaseInitializer
{
    private readonly IMigrationRunner _runner;
    private readonly ILogger<DatabaseInitializer> _logger;

    public DatabaseInitializer(IMigrationRunner runner, ILogger<DatabaseInitializer> logger)
    {
        _runner = runner;
        _logger = logger;
    }

    public void Initialize()
    {
        _logger.LogInformation("Starting database initialization...");
        
        try
        {
            // MigrateUp() will automatically skip migrations that have already been applied.
            _logger.LogInformation("Applying pending database migrations...");
            _runner.MigrateUp();
            _logger.LogInformation("Database migrations applied successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize database.");
            throw;
        }
    }
}
