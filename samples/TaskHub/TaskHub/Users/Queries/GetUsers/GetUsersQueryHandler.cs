using Contracts.Users.Queries;

namespace TaskHub.Users.Queries.GetUsers;

public class GetUsersQueryHandler(NpgsqlDataSource dataSource) : IQueryHandler<GetUsersQuery, GetUsersQuery.Result>
{
    public async Task<GetUsersQuery.Result> Handle(GetUsersQuery query, CancellationToken cancellationToken)
    {
        const string sql = "SELECT username, email FROM users.users;";

        await using var command = dataSource.CreateCommand(sql);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var results = new List<GetUsersQuery.User>();

        while (await reader.ReadAsync(cancellationToken))
        {
            results.Add(
                new GetUsersQuery.User(reader.GetFieldValue<string>(0), reader.GetFieldValue<string>(1))
            );
        }

        return new GetUsersQuery.Result(results);
    }
}
