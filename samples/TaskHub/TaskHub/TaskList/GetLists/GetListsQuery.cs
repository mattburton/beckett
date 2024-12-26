using TaskHub.Infrastructure.Database;

namespace TaskHub.TaskList.GetLists;

public class GetListsQuery : IDatabaseQuery<IReadOnlyList<GetListsReadModel>>
{
    public async Task<IReadOnlyList<GetListsReadModel>> Execute(NpgsqlCommand command, CancellationToken cancellationToken)
    {
        const string sql = "SELECT id, name FROM taskhub.task_lists;";

        command.CommandText = sql;

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var results = new List<GetListsReadModel>();

        while (await reader.ReadAsync(cancellationToken))
        {
            results.Add(
                new GetListsReadModel
                {
                    Id = reader.GetGuid(0),
                    Name = reader.GetString(1)
                }
            );
        }

        return results;
    }
}
