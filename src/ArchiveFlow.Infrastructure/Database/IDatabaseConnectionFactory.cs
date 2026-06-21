using System.Data;

namespace ArchiveFlow.Infrastructure.Database;

/// <summary>
/// Creates database connections for repositories and migration services.
/// </summary>
public interface IDatabaseConnectionFactory
{
    string DatabasePath { get; }
    string ConnectionString { get; }

    IDbConnection CreateConnection();
}