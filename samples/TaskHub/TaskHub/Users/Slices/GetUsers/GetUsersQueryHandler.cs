namespace TaskHub.Users.Slices.GetUsers;

public class GetUsersQueryHandler(
    NpgsqlDataSource dataSource
) : IQueryHandler<GetUsersQuery, IReadOnlyList<TaskLists.Slices.UserLookup.UserLookupReadModel>>
{
    public async Task<IReadOnlyList<TaskLists.Slices.UserLookup.UserLookupReadModel>?> Handle(
        GetUsersQuery query,
        CancellationToken cancellationToken
    )
    {
        const string sql = "SELECT username, email FROM users.get_users_read_model;";

        await using var command = dataSource.CreateCommand(sql);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var results = new List<TaskLists.Slices.UserLookup.UserLookupReadModel>();

        while (await reader.ReadAsync(cancellationToken))
        {
            results.Add(
                new TaskLists.Slices.UserLookup.UserLookupReadModel
                {
                    Username = reader.GetFieldValue<string>(0),
                    Email = reader.GetFieldValue<string>(1)
                }
            );
        }

        return results;
    }
}
