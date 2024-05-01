using Npgsql;

namespace Beckett.Storage.Postgres;

public class PostgresOptions
{
    internal bool Enabled { get; set; }
    internal string ConnectionString { get; private set; } = null!;
    internal string Schema { get; private set; } = "beckett";

    internal bool RunMigrationsAtStartup { get; private set; }
    internal string MigrationConnectionString { get; private set; } = null!;
    internal int MigrationAdvisoryLockId { get; private set; }

    internal bool EnableNotifications { get; set; }
    internal string ListenerConnectionString { get; private set; } = null!;

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
    }

    public void UseNotifications()
    {
        EnableNotifications = true;

        var builder = new NpgsqlConnectionStringBuilder(ConnectionString)
        {
            SearchPath = Schema,
            KeepAlive = 10
        };

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
            throw new ArgumentException("You must supply a connection string to use when migrating the database");

        MigrationAdvisoryLockId = advisoryLockId switch
        {
            < 0 => throw new ArgumentException("The migration advisory lock ID must be greater than or equal to zero"),
            _ => advisoryLockId ?? 0
        };

        RunMigrationsAtStartup = true;
    }
}
