using TaskHub.TaskLists.Events;

namespace TaskHub.TaskLists.GetLists;

public class TaskListViewProjection(NpgsqlDataSource dataSource) : IProjection<TaskListView, Guid>
{
    public void Configure(IProjectionConfiguration<Guid> configuration)
    {
        configuration.CreatedBy<TaskListAdded>(x => x.Id);
        configuration.UpdatedBy<TaskListNameChanged>(x => x.Id);
        configuration.DeletedBy<TaskListDeleted>(x => x.Id);
    }

    public async Task<TaskListView?> Load(Guid key, CancellationToken cancellationToken)
    {
        const string sql = "SELECT id, name FROM taskhub.task_list_view WHERE id = $1;";

        var command = dataSource.CreateCommand(sql);

        command.Parameters.AddWithValue(key);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        await reader.ReadAsync(cancellationToken);

        if (!reader.HasRows)
        {
            return null;
        }

        return new TaskListView
        {
            Id = reader.GetGuid(0),
            Name = reader.GetString(1)
        };
    }

    public async Task Create(TaskListView model, CancellationToken cancellationToken)
    {
        const string sql = "INSERT INTO taskhub.task_list_view (id, name) VALUES ($1, $2);";

        var command = dataSource.CreateCommand(sql);

        command.Parameters.AddWithValue(model.Id);
        command.Parameters.AddWithValue(model.Name);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task Update(TaskListView model, CancellationToken cancellationToken)
    {
        const string sql = "UPDATE taskhub.task_list_view SET name = $2 WHERE id = $1;";

        var command = dataSource.CreateCommand(sql);

        command.Parameters.AddWithValue(model.Id);
        command.Parameters.AddWithValue(model.Name);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task Delete(TaskListView model, CancellationToken cancellationToken)
    {
        const string sql = "DELETE FROM taskhub.task_list_view WHERE id = $1;";

        var command = dataSource.CreateCommand(sql);

        command.Parameters.AddWithValue(model.Id);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
