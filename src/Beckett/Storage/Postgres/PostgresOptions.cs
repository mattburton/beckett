using Npgsql;

namespace Beckett.Storage.Postgres;

public class PostgresOptions
{
    public const string DefaultSchema = "beckett";

    internal bool Enabled { get; set; }
    internal NpgsqlDataSource? DataSource { get; private set; }
    internal string Schema { get; private set; } = DefaultSchema;

    internal bool RunMigrationsAtStartup { get; private set; }
    internal string? MigrationConnectionString { get; private set; }
    internal int MigrationAdvisoryLockId { get; private set; }

    internal bool EnableNotifications { get; private set; }
    internal TimeSpan ListenerKeepAlive { get; private set; } = TimeSpan.FromSeconds(10);

    public void UseSchema(string schema)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(schema);

        Schema = schema;
    }

    public void UseDataSource(NpgsqlDataSource dataSource)
    {
        DataSource = dataSource;
    }

    public void UseConnectionString(string connectionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        var builder = new NpgsqlConnectionStringBuilder(connectionString)
        {
            SearchPath = Schema
        };

        DataSource = new NpgsqlDataSourceBuilder(builder.ConnectionString).AddBeckett().Build();
    }

    public void UseNotifications(TimeSpan? keepAlive = null)
    {
        EnableNotifications = true;

        if (keepAlive != null)
        {
            ListenerKeepAlive = keepAlive.Value;
        }
    }

    public void AutoMigrate(string? connectionString = null, int? advisoryLockId = null)
    {
        if (!string.IsNullOrWhiteSpace(connectionString))
        {
            MigrationConnectionString = connectionString;
        }

        MigrationAdvisoryLockId = advisoryLockId switch
        {
            < 0 => throw new ArgumentException("The migration advisory lock ID must be greater than or equal to zero"),
            _ => advisoryLockId ?? 0
        };

        RunMigrationsAtStartup = true;
    }
}
