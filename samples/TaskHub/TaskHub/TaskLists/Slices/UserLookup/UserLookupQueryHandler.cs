namespace TaskHub.TaskLists.Slices.UserLookup;

public class UserLookupQueryHandler(
    NpgsqlDataSource dataSource
) : IQueryHandler<UserLookupQuery, UserLookupReadModel>
{
    public async Task<UserLookupReadModel?> Handle(
        UserLookupQuery query,
        CancellationToken cancellationToken
    )
    {
        const string sql = "SELECT username, email FROM task_lists.user_lookup WHERE username = $1;";

        await using var command = dataSource.CreateCommand(sql);

        command.Parameters.AddWithValue(query.Username);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        await reader.ReadAsync(cancellationToken);

        if (!reader.HasRows)
        {
            return null;
        }

        return new UserLookupReadModel
        {
            Username = reader.GetFieldValue<string>(0),
            Email = reader.GetFieldValue<string>(1)
        };
    }
}
