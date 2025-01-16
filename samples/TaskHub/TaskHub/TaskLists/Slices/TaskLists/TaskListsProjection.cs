using TaskHub.TaskLists.Events;

namespace TaskHub.TaskLists.Slices.TaskLists;

public class TaskListsProjection(NpgsqlDataSource dataSource) : IProjection<TaskListsReadModel, Guid>
{
    public void Configure(IProjectionConfiguration<Guid> configuration)
    {
        configuration.CreatedBy<TaskListAdded>(x => x.Id);
        configuration.UpdatedBy<TaskListNameChanged>(x => x.Id);
        configuration.DeletedBy<TaskListDeleted>(x => x.Id);
    }

    public async Task Create(TaskListsReadModel state, CancellationToken cancellationToken)
    {
        const string sql = "INSERT INTO task_lists.task_lists (id, name) VALUES ($1, $2);";

        var command = dataSource.CreateCommand(sql);

        command.Parameters.AddWithValue(state.Id);
        command.Parameters.AddWithValue(state.Name);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<TaskListsReadModel?> Read(Guid key, CancellationToken cancellationToken)
    {
        const string sql = "SELECT id, name FROM task_lists.task_lists WHERE id = $1;";

        var command = dataSource.CreateCommand(sql);

        command.Parameters.AddWithValue(key);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        await reader.ReadAsync(cancellationToken);

        if (!reader.HasRows)
        {
            return null;
        }

        return new TaskListsReadModel
        {
            Id = reader.GetFieldValue<Guid>(0),
            Name = reader.GetFieldValue<string>(1)
        };
    }

    public async Task Update(TaskListsReadModel state, CancellationToken cancellationToken)
    {
        const string sql = "UPDATE task_lists.task_lists SET name = $2 WHERE id = $1;";

        var command = dataSource.CreateCommand(sql);

        command.Parameters.AddWithValue(state.Id);
        command.Parameters.AddWithValue(state.Name);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task Delete(Guid key, CancellationToken cancellationToken)
    {
        const string sql = "DELETE FROM task_lists.task_lists WHERE id = $1;";

        var command = dataSource.CreateCommand(sql);

        command.Parameters.AddWithValue(key);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
