using Contracts.TaskLists.Queries;

namespace TaskHub.TaskLists.Queries.GetTaskLists;

public class GetTaskListsQueryHandler(
    NpgsqlDataSource dataSource
) : IQueryHandler<GetTaskListsQuery, GetTaskListsQuery.Result>
{
    public async Task<GetTaskListsQuery.Result> Handle(
        GetTaskListsQuery query,
        CancellationToken cancellationToken
    )
    {
        const string sql = "SELECT id, name FROM task_lists.task_lists;";

        await using var command = dataSource.CreateCommand(sql);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var results = new List<GetTaskListsQuery.TaskList>();

        while (await reader.ReadAsync(cancellationToken))
        {
            results.Add(
                new GetTaskListsQuery.TaskList(reader.GetFieldValue<Guid>(0), reader.GetFieldValue<string>(1))
            );
        }

        return new GetTaskListsQuery.Result(results);
    }
}
