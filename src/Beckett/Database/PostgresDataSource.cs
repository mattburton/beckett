using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace Beckett.Database;

public class PostgresDataSource : IPostgresDataSource
{
    private const string DataSourceErrorMessage =
        "Registered NpgsqlDataSource not found - please register one using AddNpgsqlDataSource from the Npgsql.DependencyInjection package, provide a configured instance via UseDataSource, or call UseConnectionString";

    private readonly NpgsqlDataSource _dataSource;
    private readonly NpgsqlDataSource _messageStoreReadDataSource;
    private readonly NpgsqlDataSource _messageStoreWriteDataSource;

    public PostgresDataSource(PostgresOptions options, IServiceProvider serviceProvider)
    {
        var sharedDataSource = options.DataSource ?? serviceProvider.GetService<NpgsqlDataSource>();

        _dataSource = SetupDataSource(options, sharedDataSource);

        _messageStoreReadDataSource = SetupMessageStoreReadDataSource(options, sharedDataSource);

        _messageStoreWriteDataSource = SetupMessageStoreWriteDataSource(options, sharedDataSource);
    }

    public NpgsqlConnection CreateConnection() => _dataSource.CreateConnection();

    public NpgsqlConnection CreateMessageStoreReadConnection() => _messageStoreReadDataSource.CreateConnection();

    public NpgsqlConnection CreateMessageStoreWriteConnection() => _messageStoreWriteDataSource.CreateConnection();

    private static NpgsqlDataSource SetupDataSource(PostgresOptions options, NpgsqlDataSource? sharedDataSource)
    {
        var dataSource = options.DataSource ?? sharedDataSource;

        return dataSource ?? throw new Exception(DataSourceErrorMessage);
    }

    private static NpgsqlDataSource SetupMessageStoreReadDataSource(PostgresOptions options, NpgsqlDataSource? sharedDataSource)
    {
        var dataSource = options.MessageStoreReadDataSource ?? sharedDataSource;

        return dataSource ?? throw new Exception(DataSourceErrorMessage);
    }

    private static NpgsqlDataSource SetupMessageStoreWriteDataSource(PostgresOptions options, NpgsqlDataSource? sharedDataSource)
    {
        var dataSource = options.MessageStoreWriteDataSource ?? sharedDataSource;

        return dataSource ?? throw new Exception(DataSourceErrorMessage);
    }
}
