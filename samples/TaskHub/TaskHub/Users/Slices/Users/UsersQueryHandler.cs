namespace TaskHub.Users.Slices.Users;

public class UsersQueryHandler(
    NpgsqlDataSource dataSource
) : IQueryHandler<UsersQuery, IReadOnlyList<UsersReadModel>>
{
    public async Task<IReadOnlyList<UsersReadModel>?> Handle(
        UsersQuery query,
        CancellationToken cancellationToken
    )
    {
        const string sql = "SELECT username, email FROM users.users;";

        await using var command = dataSource.CreateCommand(sql);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var results = new List<UsersReadModel>();

        while (await reader.ReadAsync(cancellationToken))
        {
            results.Add(
                new UsersReadModel
                {
                    Username = reader.GetFieldValue<string>(0),
                    Email = reader.GetFieldValue<string>(1)
                }
            );
        }

        return results;
    }
}
