using Npgsql;

namespace Beckett.Storage.Postgres;

public interface IPostgresDatabase
{
    NpgsqlConnection CreateConnection();
}

public class PostgresDatabase(NpgsqlDataSource dataSource) : IPostgresDatabase
{
    public NpgsqlConnection CreateConnection() => dataSource.CreateConnection();
}
