namespace TaskHub.Infrastructure.Database;

public interface IDatabase
{
    Task<T> Execute<T>(IDatabaseQuery<T> query, CancellationToken cancellationToken);
}

public class Database(NpgsqlDataSource dataSource) : IDatabase
{
    public async Task<T> Execute<T>(IDatabaseQuery<T> query, CancellationToken cancellationToken)
    {
        await using var connection = dataSource.CreateConnection();

        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();

        return await query.Execute(command, cancellationToken);
    }
}
