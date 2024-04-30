using Beckett.Storage.Postgres.Types;
using Npgsql;

namespace Beckett.Storage.Postgres;

public interface IPostgresDatabase
{
    NpgsqlConnection CreateConnection();
}

public class PostgresDatabase(BeckettOptions options) : IPostgresDatabase
{
    private readonly NpgsqlDataSource _dataSource = BuildDataSource(options);

    public NpgsqlConnection CreateConnection() => _dataSource.CreateConnection();

    private static NpgsqlDataSource BuildDataSource(BeckettOptions options)
    {
        var builder = new NpgsqlDataSourceBuilder(options.Postgres.ConnectionString);

        builder.MapComposite<NewStreamEvent>($"{options.Postgres.Schema}.new_stream_event");

        return builder.Build();
    }
}


