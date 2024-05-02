using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace Beckett.Storage.Postgres;

internal interface IPostgresDatabase
{
    NpgsqlConnection CreateConnection();
}

internal class PostgresDatabase : IPostgresDatabase
{
    private readonly NpgsqlDataSource _dataSource;

    public PostgresDatabase(BeckettOptions options, IServiceProvider serviceProvider)
    {
        if (options.Postgres.DataSource != null)
        {
            _dataSource = options.Postgres.DataSource;
        }

        var dataSource = serviceProvider.GetService<NpgsqlDataSource>();

        _dataSource = dataSource ?? throw new InvalidOperationException(
            "Registered NpgsqlDataSource not found - please register one using AddNpgsqlDataSource from the Npgsql.DependencyInjection package, provide a configured instance via UseDataSource, or call UseConnectionString"
        );
    }

    public NpgsqlConnection CreateConnection() => _dataSource.CreateConnection();
}
