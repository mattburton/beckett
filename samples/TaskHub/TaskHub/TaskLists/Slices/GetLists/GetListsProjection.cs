using TaskHub.TaskLists.Events;

namespace TaskHub.TaskLists.Slices.GetLists;

public class GetListsProjection(NpgsqlDataSource dataSource) : IProjection<GetListsReadModel, Guid>
{
    public void Configure(IProjectionConfiguration<Guid> configuration)
    {
        configuration.CreatedBy<TaskListAdded>(x => x.Id);
        configuration.UpdatedBy<TaskListNameChanged>(x => x.Id);
        configuration.DeletedBy<TaskListDeleted>(x => x.Id);
    }

    public async Task Create(GetListsReadModel state, CancellationToken cancellationToken)
    {
        const string sql = "INSERT INTO task_lists.get_lists_read_model (id, name) VALUES ($1, $2);";

        var command = dataSource.CreateCommand(sql);

        command.Parameters.AddWithValue(state.Id);
        command.Parameters.AddWithValue(state.Name);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<GetListsReadModel?> Read(Guid key, CancellationToken cancellationToken)
    {
        const string sql = "SELECT id, name FROM task_lists.get_lists_read_model WHERE id = $1;";

        var command = dataSource.CreateCommand(sql);

        command.Parameters.AddWithValue(key);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        await reader.ReadAsync(cancellationToken);

        if (!reader.HasRows)
        {
            return null;
        }

        return new GetListsReadModel
        {
            Id = reader.GetFieldValue<Guid>(0),
            Name = reader.GetFieldValue<string>(1)
        };
    }

    public async Task Update(GetListsReadModel state, CancellationToken cancellationToken)
    {
        const string sql = "UPDATE task_lists.get_lists_read_model SET name = $2 WHERE id = $1;";

        var command = dataSource.CreateCommand(sql);

        command.Parameters.AddWithValue(state.Id);
        command.Parameters.AddWithValue(state.Name);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task Delete(Guid key, CancellationToken cancellationToken)
    {
        const string sql = "DELETE FROM task_lists.get_lists_read_model WHERE id = $1;";

        var command = dataSource.CreateCommand(sql);

        command.Parameters.AddWithValue(key);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
