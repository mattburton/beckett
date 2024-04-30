using Npgsql;

namespace Beckett.Database;

public class DatabaseOptions
{
    internal bool RunMigrationsAtStartup { get; private set; }
    internal string MigrationConnectionString { get; private set; } = null!;
    internal string ConnectionString { get; private set; } = null!;
    internal string ListenerConnectionString { get; private set; } = null!;
    internal string Schema { get; private set; } = "event_store";
    internal int MigrationAdvisoryLockId { get; private set; }

    public void UseSchema(string schema)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(schema);

        Schema = schema;
    }

    public void UseConnectionString(string connectionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        var builder = new NpgsqlConnectionStringBuilder(connectionString)
        {
            SearchPath = Schema
        };

        ConnectionString = builder.ConnectionString;

        builder.KeepAlive = 10;

        ListenerConnectionString = builder.ConnectionString;
    }

    public void AutoMigrate(string? connectionString = null, int? advisoryLockId = null)
    {
        if (!string.IsNullOrWhiteSpace(connectionString))
        {
            var builder = new NpgsqlConnectionStringBuilder(connectionString)
            {
                SearchPath = Schema
            };

            MigrationConnectionString = builder.ConnectionString;

            return;
        }

        MigrationConnectionString = ConnectionString ??
            throw new ArgumentException("Please configure the connection string prior in order to run migrations");

        MigrationAdvisoryLockId = advisoryLockId switch
        {
            < 0 => throw new ArgumentException("The migration advisory lock ID must be greater than or equal to zero"),
            _ => advisoryLockId ?? 0
        };

        RunMigrationsAtStartup = true;
    }
}
