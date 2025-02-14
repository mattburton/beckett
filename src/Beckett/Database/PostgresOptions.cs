using Npgsql;

namespace Beckett.Database;

public class PostgresOptions
{
    public const string DefaultSchema = "beckett";

    /// <summary>
    /// The Postgres database schema to use for Beckett.
    /// </summary>
    public string Schema { get; private set; } = DefaultSchema;

    /// <summary>
    /// Prepare statements prior to their execution. When using client-side connection pooling this is recommended as
    /// it will increase performance, however if you are using an external connection pool such as pgbouncer then this
    /// should be disabled. Enabled by default.
    /// </summary>
    public bool PrepareStatements { get; set; } = true;

    /// <summary>
    /// Refresh interval for tenant materialized view used by the dashboard. Runs every hour by default.
    /// </summary>
    public TimeSpan TenantRefreshInterval { get; set; } = TimeSpan.FromHours(1);

    internal NpgsqlDataSource? DataSource { get; private set; }
    internal NpgsqlDataSource? MessageStoreReadDataSource { get; private set; }
    internal NpgsqlDataSource? MessageStoreWriteDataSource { get; private set; }
    internal NpgsqlDataSource? NotificationsDataSource { get; private set; }

    /// <summary>
    /// Configure the database schema where Beckett will create the tables and functions it uses at runtime. Defaults to
    /// <c>beckett</c> if not specified.
    /// </summary>
    /// <param name="schema"></param>
    public void UseSchema(string schema)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(schema);

        Schema = schema;
    }

    /// <summary>
    /// Configure the <see cref="NpgsqlDataSource"/> that Beckett should use. This data source must have had the Beckett
    /// types configured while building it - this can be done using the <c>AddBeckett()</c> extension method on
    /// <see cref="NpgsqlDataSourceBuilder"/>.
    /// </summary>
    /// <param name="dataSource"></param>
    public void UseDataSource(NpgsqlDataSource dataSource) => DataSource = dataSource;

    /// <summary>
    /// Configure the <see cref="NpgsqlDataSource"/> that is used when reading from Beckett's Postgres message store.
    /// </summary>
    /// <param name="dataSource"></param>
    public void UseMessageStoreReadDataSource(NpgsqlDataSource dataSource) => MessageStoreReadDataSource = dataSource;

    /// <summary>
    /// Configure the <see cref="NpgsqlDataSource"/> that is used when writing to Beckett's Postgres message store. This
    /// data source must have had the Beckett types configured while building it - this can be done using the
    /// <c>AddBeckett()</c> extension method on <see cref="NpgsqlDataSourceBuilder"/>.
    /// </summary>
    /// <param name="dataSource"></param>
    public void UseMessageStoreWriteDataSource(NpgsqlDataSource dataSource) => MessageStoreWriteDataSource = dataSource;

    /// <summary>
    /// Configure the <see cref="NpgsqlDataSource"/> that Beckett should use for notifications.
    /// </summary>
    /// <param name="dataSource"></param>
    public void UseNotificationsDataSource(NpgsqlDataSource dataSource) => NotificationsDataSource = dataSource;

    /// <summary>
    /// Configure a specific database connection string for Beckett to use.
    /// </summary>
    /// <param name="connectionString"></param>
    public void UseConnectionString(string connectionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        var builder = new NpgsqlConnectionStringBuilder(connectionString)
        {
            SearchPath = Schema
        };

        DataSource = new NpgsqlDataSourceBuilder(builder.ConnectionString).AddBeckett().Build();
    }

    /// <summary>
    /// Configure a specific database connection string for Beckett to use when reading from the Postgres message store.
    /// This allows you to configure Beckett to use a separate connection for reading from follower instance(s) when
    /// replicas are in use.
    /// </summary>
    /// <param name="connectionString"></param>
    public void UseMessageStoreReadConnectionString(string connectionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        var builder = new NpgsqlConnectionStringBuilder(connectionString)
        {
            SearchPath = Schema
        };

        MessageStoreReadDataSource = new NpgsqlDataSourceBuilder(builder.ConnectionString).AddBeckett().Build();
    }

    /// <summary>
    /// Configure a specific database connection string for Beckett to use when writing to the Postgres message store.
    /// This is the primary / leader instance when replicas are in use.
    /// </summary>
    /// <param name="connectionString"></param>
    public void UseMessageStoreWriteConnectionString(string connectionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        var builder = new NpgsqlConnectionStringBuilder(connectionString)
        {
            SearchPath = Schema
        };

        MessageStoreReadDataSource = new NpgsqlDataSourceBuilder(builder.ConnectionString).AddBeckett().Build();
    }

    /// <summary>
    /// Enable Beckett to listen to Postgres notifications as an efficient alternative to polling. When using
    /// notifications it is recommended to reduce the global and checkpoint polling intervals so they occur less
    /// frequently, primarily serving as a fallback in case there is a problem receiving them. Beckett creates a single
    /// connection to receive all notifications, so pooling can be disabled for this connection. It is also recommended
    /// to enable keepalives - example for Npgsql with pooling disabled and a 10 second keepalive:
    /// <code>
    /// Server=localhost;Database=postgres;User Id=postgres;Password=password;Pooling=false;Keepalive=10;
    /// </code>
    /// </summary>
    /// <param name="connectionString"></param>
    public void UseNotificationsConnectionString(string connectionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        var builder = new NpgsqlConnectionStringBuilder(connectionString)
        {
            SearchPath = Schema
        };

        NotificationsDataSource = new NpgsqlDataSourceBuilder(builder.ConnectionString).Build();
    }
}
