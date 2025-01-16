namespace TaskHub.TaskLists.Slices.GetLists;

public class GetListsQueryHandler(
    NpgsqlDataSource dataSource
) : IQueryHandler<GetListsQuery, IReadOnlyList<GetListsReadModel>>
{
    public async Task<IReadOnlyList<GetListsReadModel>?> Handle(
        GetListsQuery query,
        CancellationToken cancellationToken
    )
    {
        const string sql = "SELECT id, name FROM task_lists.get_lists_read_model;";

        await using var command = dataSource.CreateCommand(sql);

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
}
