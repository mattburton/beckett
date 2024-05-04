using Npgsql;

namespace Beckett.Storage.Postgres;

public class PostgresOptions
{
    public const string DefaultSchema = "beckett";

    public bool Enabled { get; set; }
    public NpgsqlDataSource? DataSource { get; private set; }
    public string Schema { get; private set; } = DefaultSchema;
    public bool EnableNotifications { get; private set; }
    public TimeSpan ListenerKeepAlive { get; private set; } = TimeSpan.FromSeconds(10);

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
}
