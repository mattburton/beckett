namespace TaskHub.Infrastructure.Database;

public interface IDatabaseQuery<T>
{
    Task<T> Execute(NpgsqlCommand command, CancellationToken cancellationToken);
}
