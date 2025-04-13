using NpgsqlTypes;
using TaskLists.Events;

namespace TaskLists.GetLists;

public class GetListsProjection(NpgsqlDataSource dataSource) : PostgresProjection<GetListsReadModel>(dataSource)
{
    public override void Configure(IProjectionConfiguration configuration)
    {
        configuration.CreatedBy<TaskListAdded>(x => x.Id);
        configuration.UpdatedBy<TaskListNameChanged>(x => x.Id);
        configuration.DeletedBy<TaskListDeleted>(x => x.Id);
    }

    public override object GetKey(GetListsReadModel state) => state.Id;

    protected override async Task<IReadOnlyList<GetListsReadModel>> Load(
        IReadOnlyList<object> keys,
        NpgsqlDataSource dataSource,
        CancellationToken cancellationToken
    )
    {
        const string sql = "SELECT id, name FROM task_lists.task_lists WHERE id = ANY($1);";

        var command = dataSource.CreateCommand(sql);

        command.Parameters.AddWithValue(NpgsqlDbType.Uuid | NpgsqlDbType.Array, keys);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var results = new List<GetListsReadModel>();

        while (await reader.ReadAsync(cancellationToken))
        {
            results.Add(
                new GetListsReadModel
                {
                    Id = reader.GetFieldValue<Guid>(0),
                    Name = reader.GetFieldValue<string>(1)
                }
            );
        }

        return results;
    }

    protected override NpgsqlBatchCommand SaveCommand(GetListsReadModel state)
    {
        const string sql = """
            INSERT INTO task_lists.task_lists (id, name)
            VALUES ($1, $2)
            ON CONFLICT (id) DO UPDATE SET name = $2;
        """;

        var command = new NpgsqlBatchCommand(sql);

        command.Parameters.AddWithValue(state.Id);
        command.Parameters.AddWithValue(state.Name);

        return command;
    }

    protected override NpgsqlBatchCommand DeleteCommand(GetListsReadModel state)
    {
        const string sql = "DELETE FROM task_lists.task_lists WHERE id = $1;";

        var command = new NpgsqlBatchCommand(sql);

        command.Parameters.AddWithValue(state.Id);

        return command;
    }
}
