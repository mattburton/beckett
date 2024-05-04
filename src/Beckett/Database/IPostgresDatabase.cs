using Npgsql;

namespace Beckett.Database;

public interface IPostgresDatabase
{
    NpgsqlConnection CreateConnection();

    Task<T> Execute<T>(IPostgresDatabaseQuery<T> query, CancellationToken cancellationToken);
    Task<T> Execute<T>(IPostgresDatabaseQuery<T> query, NpgsqlConnection connection, CancellationToken cancellationToken);
    Task<T> Execute<T>(IPostgresDatabaseQuery<T> query, NpgsqlConnection connection, NpgsqlTransaction transaction, CancellationToken cancellationToken);
}
