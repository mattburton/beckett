using TaskHub.TaskLists.Events;

namespace TaskHub.TaskLists.GetLists;

public class GetListsProjection(NpgsqlDataSource dataSource) : IProjection<GetListsReadModel, Guid>
{
    public void Configure(IProjectionConfiguration<Guid> configuration)
    {
        configuration.CreatedBy<TaskListAdded>(x => x.Id);
        configuration.UpdatedBy<TaskListNameChanged>(x => x.Id);
        configuration.DeletedBy<TaskListDeleted>(x => x.Id);
    }

    public async Task<GetListsReadModel?> Load(Guid key, CancellationToken cancellationToken)
    {
        const string sql = "SELECT id, name FROM taskhub.task_lists WHERE id = $1;";

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
            Id = reader.GetGuid(0),
            Name = reader.GetString(1)
        };
    }

    public async Task Create(GetListsReadModel model, CancellationToken cancellationToken)
    {
        const string sql = "INSERT INTO taskhub.task_lists (id, name) VALUES ($1, $2);";

        var command = dataSource.CreateCommand(sql);

        command.Parameters.AddWithValue(model.Id);
        command.Parameters.AddWithValue(model.Name);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task Update(GetListsReadModel model, CancellationToken cancellationToken)
    {
        const string sql = "UPDATE taskhub.task_lists SET name = $2 WHERE id = $1;";

        var command = dataSource.CreateCommand(sql);

        command.Parameters.AddWithValue(model.Id);
        command.Parameters.AddWithValue(model.Name);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task Delete(GetListsReadModel model, CancellationToken cancellationToken)
    {
        const string sql = "DELETE FROM taskhub.task_lists WHERE id = $1;";

        var command = dataSource.CreateCommand(sql);

        command.Parameters.AddWithValue(model.Id);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
