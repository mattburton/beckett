namespace TaskHub.TaskLists.Slices.TaskLists;

public class TaskListsQueryHandler(
    NpgsqlDataSource dataSource
) : IQueryHandler<TaskListsQuery, IReadOnlyList<TaskListsReadModel>>
{
    public async Task<IReadOnlyList<TaskListsReadModel>?> Handle(
        TaskListsQuery query,
        CancellationToken cancellationToken
    )
    {
        const string sql = "SELECT id, name FROM task_lists.task_lists;";

        await using var command = dataSource.CreateCommand(sql);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var results = new List<TaskListsReadModel>();

        while (await reader.ReadAsync(cancellationToken))
        {
            results.Add(
                new TaskListsReadModel
                {
                    Id = reader.GetFieldValue<Guid>(0),
                    Name = reader.GetFieldValue<string>(1)
                }
            );
        }

        return results;
    }
}
