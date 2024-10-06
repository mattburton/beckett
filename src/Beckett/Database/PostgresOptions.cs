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
    /// Configure whether to use Postgres notifications (via NOTIFY / LISTEN) to trigger Beckett to look for work to do
    /// in various situations (new messages available, new retries, etc...) This can be far more efficient than polling
    /// but may not be supported in all database deployment scenarios. Enabled by default.
    /// </summary>
    public bool Notifications { get; set; } = true;

    /// <summary>
    /// Prepare statements prior to their execution. When using client-side connection pooling this is recommended as
    /// it will increase performance, however if you are using an external connection pool such as pgbouncer then this
    /// should be disabled. Enabled by default.
    /// </summary>
    public bool PrepareStatements { get; set; } = true;

    internal NpgsqlDataSource? DataSource { get; private set; }

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
}
