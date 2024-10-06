using Npgsql;

namespace Beckett.Database;

public class PostgresDatabase(NpgsqlDataSource dataSource) : IPostgresDatabase
{
    public NpgsqlConnection CreateConnection() => dataSource.CreateConnection();

    public async Task<T> Execute<T>(IPostgresDatabaseQuery<T> query, CancellationToken cancellationToken)
    {
        await using var connection = CreateConnection();

        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();

        return await query.Execute(command, cancellationToken);
    }

    public async Task<T> Execute<T>(
        IPostgresDatabaseQuery<T> query,
        NpgsqlConnection connection,
        CancellationToken cancellationToken
    )
    {
        await using var command = connection.CreateCommand();

        return await query.Execute(command, cancellationToken);
    }

    public async Task<T> Execute<T>(
        IPostgresDatabaseQuery<T> query,
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        CancellationToken cancellationToken
    )
    {
        await using var command = connection.CreateCommand();

        command.Transaction = transaction;

        return await query.Execute(command, cancellationToken);
    }
}
