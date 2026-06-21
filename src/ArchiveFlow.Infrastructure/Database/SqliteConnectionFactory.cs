using System.Data;
using Microsoft.Data.Sqlite;

namespace ArchiveFlow.Infrastructure.Database;

/// <summary>
/// Centralized SQLite connection factory.
/// Keeps the database path consistent across migrations and repositories.
/// </summary>
public sealed class SqliteConnectionFactory : IDatabaseConnectionFactory
{
    public string DatabasePath { get; }
    public string ConnectionString { get; }

    public SqliteConnectionFactory()
    {
        DatabasePath = ResolveProjectLocalDatabasePath();
        Directory.CreateDirectory(Path.GetDirectoryName(DatabasePath)!);

        ConnectionString = $"Data Source={DatabasePath};";
    }

    public IDbConnection CreateConnection()
    {
        return new SqliteConnection(ConnectionString);
    }

    private static string ResolveProjectLocalDatabasePath()
    {
        var current = Directory.GetCurrentDirectory();

        while (!string.IsNullOrWhiteSpace(current))
        {
            if (File.Exists(Path.Combine(current, "ArchiveFlow.sln")))
            {
                return Path.Combine(current, "Data", "archiveflow.db");
            }

            var parent = Directory.GetParent(current);
            if (parent == null)
            {
                break;
            }

            current = parent.FullName;
        }

        return Path.Combine(Directory.GetCurrentDirectory(), "Data", "archiveflow.db");
    }
}