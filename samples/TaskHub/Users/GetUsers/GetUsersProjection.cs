using NpgsqlTypes;
using Users.Events;

namespace Users.GetUsers;

public class GetUsersProjection(NpgsqlDataSource dataSource) : PostgresProjection<GetUsersReadModel>(dataSource)
{
    public override void Configure(IProjectionConfiguration configuration)
    {
        configuration.CreatedBy<UserRegistered>(x => x.Username);
        configuration.DeletedBy<UserDeleted>(x => x.Username);
    }

    public override object GetKey(GetUsersReadModel state) => state.Username;

    protected override async Task<IReadOnlyList<GetUsersReadModel>> Load(
        IReadOnlyList<object> keys,
        NpgsqlDataSource dataSource,
        CancellationToken cancellationToken
    )
    {
        const string sql = "SELECT username, email FROM users.users WHERE username = ANY($1);";

        await using var command = dataSource.CreateCommand(sql);

        command.Parameters.AddWithValue(NpgsqlDbType.Text | NpgsqlDbType.Array, keys);

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

    protected override NpgsqlBatchCommand SaveCommand(GetUsersReadModel state)
    {
        const string sql = """
            INSERT INTO users.users (username, email)
            VALUES ($1, $2)
            ON CONFLICT (username) DO UPDATE SET email = $2;
        """;

        var command = new NpgsqlBatchCommand(sql);

        command.Parameters.AddWithValue(state.Username);
        command.Parameters.AddWithValue(state.Email);

        return command;
    }

    protected override NpgsqlBatchCommand DeleteCommand(GetUsersReadModel state)
    {
        const string sql = "DELETE FROM users.users WHERE username = $1;";

        var command = new NpgsqlBatchCommand(sql);

        command.Parameters.AddWithValue(state.Username);

        return command;
    }
}
