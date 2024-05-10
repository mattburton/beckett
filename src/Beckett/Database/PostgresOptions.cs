using Npgsql;

namespace Beckett.Database;

public class PostgresOptions
{
    public const string DefaultSchema = "beckett";

    internal NpgsqlDataSource? DataSource { get; private set; }

    public bool Enabled { get; set; } = true;
    public bool Notifications { get; set; } = true;
    public string Schema { get; set; } = DefaultSchema;

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
}
