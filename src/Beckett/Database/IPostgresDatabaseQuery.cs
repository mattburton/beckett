using Npgsql;

namespace Beckett.Database;

public interface IPostgresDatabaseQuery<T>
{
    Task<T> Execute(NpgsqlCommand command, CancellationToken cancellationToken);
}
