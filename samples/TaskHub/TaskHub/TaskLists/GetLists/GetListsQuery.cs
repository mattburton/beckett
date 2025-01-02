namespace TaskHub.TaskLists.GetLists;

public class GetListsQuery : IDatabaseQuery<IReadOnlyList<TaskListView>>
{
    public async Task<IReadOnlyList<TaskListView>> Execute(NpgsqlCommand command, CancellationToken cancellationToken)
    {
        const string sql = "SELECT id, name FROM taskhub.task_list_view;";

        command.CommandText = sql;

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var results = new List<TaskListView>();

        while (await reader.ReadAsync(cancellationToken))
        {
            results.Add(
                new TaskListView
                {
                    Id = reader.GetGuid(0),
                    Name = reader.GetString(1)
                }
            );
        }

        return results;
    }
}
