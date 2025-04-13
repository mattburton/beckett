using Core.State;
using Npgsql;

namespace Core.Projections;

public abstract class PostgresProjection<TState>(NpgsqlDataSource dataSource)
    : IProjection<TState> where TState : class, IStateView, new()
{
    private readonly Dictionary<TState, NpgsqlBatchCommand> _commands = [];

    public abstract void Configure(IProjectionConfiguration configuration);

    public async Task<IReadOnlyList<TState>> Load(IEnumerable<object> keys, CancellationToken cancellationToken)
    {
        return await Load(keys.ToList(), dataSource, cancellationToken);
    }

    public abstract object GetKey(TState state);

    public void Save(TState state) => _commands[state] = SaveCommand(state);

    public void Delete(TState state) => _commands[state] = DeleteCommand(state);

    public async Task SaveChanges(CancellationToken cancellationToken)
    {
        await using var connection = dataSource.CreateConnection();

        await connection.OpenAsync(cancellationToken);

        var batch = new NpgsqlBatch(connection);

        foreach (var command in _commands.Values)
        {
            batch.BatchCommands.Add(command);
        }

        await batch.ExecuteNonQueryAsync(cancellationToken);
    }

    protected abstract Task<IReadOnlyList<TState>> Load(
        IReadOnlyList<object> keys,
        NpgsqlDataSource dataSource,
        CancellationToken cancellationToken
    );

    protected abstract NpgsqlBatchCommand SaveCommand(TState state);

    protected abstract NpgsqlBatchCommand DeleteCommand(TState state);
}
