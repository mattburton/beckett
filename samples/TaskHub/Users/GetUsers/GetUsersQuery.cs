namespace Users.GetUsers;

public record GetUsersQuery : IQuery<IReadOnlyList<GetUsersReadModel>>
{
    public class Handler(NpgsqlDataSource dataSource) : IQueryHandler<GetUsersQuery, IReadOnlyList<GetUsersReadModel>>
    {
        public async Task<IReadOnlyList<GetUsersReadModel>?> Handle(GetUsersQuery query, CancellationToken cancellationToken)
        {
            const string sql = "SELECT username, email FROM users.users;";

            await using var command = dataSource.CreateCommand(sql);

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);

            var results = new List<GetUsersReadModel>();

            while (await reader.ReadAsync(cancellationToken))
            {
                results.Add(
                    new GetUsersReadModel
                    {
                        Username = reader.GetFieldValue<string>(0),
                        Email = reader.GetFieldValue<string>(1)
                    }
                );
            }

            return results;
        }
    }
}
