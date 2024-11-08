using Npgsql;

namespace Beckett.Database;

public interface IPostgresDataSource
{
    NpgsqlConnection CreateConnection();
    NpgsqlConnection CreateMessageStoreReadConnection();
    NpgsqlConnection CreateMessageStoreWriteConnection();
}
