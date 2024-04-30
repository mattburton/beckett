using Beckett.Database.Types;
using Npgsql;

namespace Beckett.Database;

public interface IDataSource
{
    NpgsqlConnection CreateConnection();
}

public class DataSource(BeckettOptions options) : IDataSource
{
    private readonly NpgsqlDataSource _dataSource = BuildDataSource(options);

    public NpgsqlConnection CreateConnection() => _dataSource.CreateConnection();

    private static NpgsqlDataSource BuildDataSource(BeckettOptions options)
    {
        var builder = new NpgsqlDataSourceBuilder(options.Database.ConnectionString);

        builder.MapComposite<NewStreamEvent>($"{options.Database.Schema}.new_stream_event");

        return builder.Build();
    }
}


